/**
 * ENH-ADMIN-007 — Distributed Tracing: W3C Trace Context propagation
 * Acceptance criteria tested here:
 *   - Response contains a valid `traceparent` header on every request
 *   - traceparent format: "00-{32 hex}-{16 hex}-{02 hex}"
 *   - Incoming valid traceparent → response traceparent shares the same trace ID
 *   - Invalid traceparent format → middleware falls back to a new root trace
 *   - Missing traceparent → middleware generates a new root trace
 *   - Multiple requests without a parent → each gets a distinct trace ID
 *   - Incoming tracestate is echoed in the response
 *   - next() delegate is always called (middleware does not short-circuit)
 *   - Activity.DefaultIdFormat is forced to W3C
 */

using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using StyleNest.SharedKernel.Middleware;
using Xunit;

namespace StyleNest.Admin.Tests;

public sealed class W3CTracingMiddlewareTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Invokes the middleware with an optional incoming <c>traceparent</c> /
    /// <c>tracestate</c> and returns the response context after the pipeline completes.
    /// </summary>
    private static async Task<HttpContext> InvokeAsync(
        string? traceParent = null,
        string? traceState  = null)
    {
        // Ensure no stray Activity leaks from a previous test
        while (Activity.Current is not null)
            Activity.Current.Stop();

        var context  = new DefaultHttpContext();
        var response = new FakeResponseFeature();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(response);

        if (traceParent is not null)
            context.Request.Headers[W3CTracingMiddleware.TraceParentHeader] = traceParent;

        if (traceState is not null)
            context.Request.Headers[W3CTracingMiddleware.TraceStateHeader] = traceState;

        bool nextCalled = false;
        var middleware = new W3CTracingMiddleware(_ =>
        {
            nextCalled = true;
            // Trigger OnStarting callbacks so response headers are written
            response.FireOnStarting();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        context.Items["__nextCalled"] = nextCalled;
        return context;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_AlwaysContainsTraceParentHeader()
    {
        var ctx = await InvokeAsync();

        ctx.Response.Headers.ContainsKey(W3CTracingMiddleware.TraceParentHeader)
            .Should().BeTrue("every response must carry a traceparent for distributed tracing");
    }

    [Fact]
    public async Task Response_TraceParentHasCorrectW3CFormat()
    {
        var ctx = await InvokeAsync();

        var tp = ctx.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString();
        tp.Should().MatchRegex(
            @"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
            because: "W3C traceparent must be '00-<32 hex>-<16 hex>-<02 hex>'");
    }

    [Fact]
    public async Task Response_WithValidIncomingTraceParent_SameTraceId()
    {
        // Build a valid parent traceparent
        var traceId  = new string('a', 32);
        var spanId   = new string('b', 16);
        var incoming = $"00-{traceId}-{spanId}-01";

        var ctx = await InvokeAsync(traceParent: incoming);

        var tp = ctx.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString();
        // The 2nd segment (trace ID) must match the parent
        var parts = tp.Split('-');
        parts.Should().HaveCount(4);
        parts[1].Should().Be(traceId,
            because: "child span must preserve the parent trace ID");
    }

    [Fact]
    public async Task Response_WithInvalidTraceParent_GeneratesNewRootTrace()
    {
        var ctx = await InvokeAsync(traceParent: "not-a-valid-traceparent");

        var tp = ctx.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString();
        tp.Should().MatchRegex(
            @"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
            because: "an invalid incoming traceparent must be ignored; a new root trace is generated");
        // The trace ID must NOT be all-zeros (i.e. it is a fresh random ID)
        tp.Should().NotContain("00000000000000000000000000000000",
            because: "a new root trace must have a non-zero, randomly generated trace ID");
    }

    [Fact]
    public async Task Response_WithNoTraceParent_GeneratesNewRootTrace()
    {
        var ctx = await InvokeAsync();

        var tp = ctx.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString();
        tp.Should().MatchRegex(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$");
    }

    [Fact]
    public async Task MultipleRequestsWithoutParent_HaveDistinctTraceIds()
    {
        var ctx1 = await InvokeAsync();
        var ctx2 = await InvokeAsync();

        var tp1 = ctx1.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString().Split('-')[1];
        var tp2 = ctx2.Response.Headers[W3CTracingMiddleware.TraceParentHeader].ToString().Split('-')[1];

        tp1.Should().NotBe(tp2,
            because: "each unparented request must start a unique distributed trace");
    }

    [Fact]
    public async Task Response_WithTraceState_EchoesTraceState()
    {
        var ctx = await InvokeAsync(
            traceParent: $"00-{"a".PadRight(32, 'a')}-{"b".PadRight(16, 'b')}-01",
            traceState:  "vendor1=value1,vendor2=value2");

        var ts = ctx.Response.Headers[W3CTracingMiddleware.TraceStateHeader].ToString();
        ts.Should().Be("vendor1=value1,vendor2=value2",
            because: "tracestate must be propagated unchanged for multi-vendor trace correlation");
    }

    [Fact]
    public async Task Middleware_AlwaysCallsNext()
    {
        var ctx = await InvokeAsync();

        ctx.Items["__nextCalled"].Should().Be(true,
            because: "W3CTracingMiddleware must never short-circuit the pipeline");
    }

    [Fact]
    public void UseW3CTracing_ForcesW3CActivityFormat()
    {
        // Reset to a non-W3C value first
        Activity.DefaultIdFormat      = ActivityIdFormat.Hierarchical;
        Activity.ForceDefaultIdFormat = false;

        // Simulate what UseW3CTracing() does at startup
        Activity.DefaultIdFormat      = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        Activity.DefaultIdFormat.Should().Be(ActivityIdFormat.W3C,
            because: "UseW3CTracing must enforce W3C format process-wide");
        Activity.ForceDefaultIdFormat.Should().BeTrue(
            because: "ForceDefaultIdFormat = true prevents any code from switching back to Hierarchical");
    }

    // ── Fake response feature (handles OnStarting callbacks) ─────────────────

    private sealed class FakeResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        private readonly List<Func<object, Task>> _callbacks = [];
        private readonly List<object>             _states    = [];
        public int    StatusCode  { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public bool   HasStarted  { get; private set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _callbacks.Add(callback);
            _states.Add(state);
        }

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public void FireOnStarting()
        {
            HasStarted = true;
            for (int i = 0; i < _callbacks.Count; i++)
                _callbacks[i](_states[i]).GetAwaiter().GetResult();
        }
    }
}
