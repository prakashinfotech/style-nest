using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StyleNest.Order.API.Filters;
using Xunit;

namespace StyleNest.Order.Tests;

/// <summary>ENH-PAY-003 — Idempotency-Key header deduplication tests.</summary>
public sealed class IdempotencyFilterTests
{
    private readonly IDistributedCache _cache;
    private readonly IdempotencyFilter _sut;

    public IdempotencyFilterTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _sut   = new IdempotencyFilter(_cache, NullLogger<IdempotencyFilter>.Instance);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ActionExecutingContext MakeContext(HttpContext ctx) =>
        new(new ActionContext(ctx, new RouteData(), new ActionDescriptor()),
            [], new Dictionary<string, object?>(), new object());

    private static ActionExecutedContext MakeExecuted(HttpContext ctx, IActionResult result) =>
        new(new ActionContext(ctx, new RouteData(), new ActionDescriptor()), [], new object())
            { Result = result };

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NoHeader_PassesThrough_DelegateIsCalled()
    {
        var httpContext = new DefaultHttpContext();
        var context     = MakeContext(httpContext);
        var called      = false;

        ActionExecutionDelegate next = () =>
        {
            called = true;
            return Task.FromResult(MakeExecuted(httpContext, new OkResult()));
        };

        await _sut.OnActionExecutionAsync(context, next);

        called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidUuid_ReturnsBadRequest_DelegateNotCalled()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = "not-a-uuid";
        var context = MakeContext(httpContext);

        await _sut.OnActionExecutionAsync(context,
            () => throw new InvalidOperationException("delegate must not be called"));

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidKey_FirstCall_DelegateCalledAndResponseCached()
    {
        var key         = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = key.ToString();
        var context = MakeContext(httpContext);
        var called  = false;

        ActionExecutionDelegate next = () =>
        {
            called = true;
            return Task.FromResult(MakeExecuted(httpContext, new OkObjectResult(new { id = 1 })));
        };

        await _sut.OnActionExecutionAsync(context, next);

        called.Should().BeTrue();
        var cacheKey = $"idempotency:{key:N}";
        var cached   = await _cache.GetAsync(cacheKey);
        cached.Should().NotBeNull("first-call response should be stored in cache");
    }

    [Fact]
    public async Task ValidKey_DuplicateCall_ReturnsCachedBodyWithReplayHeader()
    {
        var key      = Guid.NewGuid();
        var cacheKey = $"idempotency:{key:N}";
        var body     = "{\"id\":99}";
        await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(body));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = key.ToString();
        var context = MakeContext(httpContext);

        await _sut.OnActionExecutionAsync(context,
            () => throw new InvalidOperationException("delegate must not be called on replay"));

        var result = context.Result.Should().BeOfType<ContentResult>().Subject;
        result.Content.Should().Be(body);
        httpContext.Response.Headers["X-Idempotent-Replayed"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task ServerError_ResponseIsNotCached()
    {
        var key         = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = key.ToString();
        var context = MakeContext(httpContext);

        ActionExecutionDelegate next = () =>
            Task.FromResult(MakeExecuted(httpContext,
                new ObjectResult("oops") { StatusCode = 500 }));

        await _sut.OnActionExecutionAsync(context, next);

        var cacheKey = $"idempotency:{key:N}";
        var cached   = await _cache.GetAsync(cacheKey);
        cached.Should().BeNull("5xx responses must not be cached so clients can retry");
    }

    [Fact]
    public async Task UuidInAlternateFormat_TreatedAsTheSameKey()
    {
        var key      = Guid.NewGuid();
        var cacheKey = $"idempotency:{key:N}";
        var body     = "{\"ok\":true}";
        await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(body));

        // Header sent with braces format {xxxxxxxx-...}
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = key.ToString("B");
        var context = MakeContext(httpContext);

        await _sut.OnActionExecutionAsync(context,
            () => throw new InvalidOperationException("must not be called"));

        context.Result.Should().BeOfType<ContentResult>()
            .Which.Content.Should().Be(body);
    }
}
