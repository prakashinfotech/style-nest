using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace StyleNest.Order.API.Filters;

/// <summary>
/// ENH-PAY-003 — Idempotency-Key header deduplication.
/// Absent header → pass-through. Invalid UUID → 400. Duplicate → replay cached response.
/// </summary>
public sealed class IdempotencyFilter(
    IDistributedCache cache,
    ILogger<IdempotencyFilter> logger) : IAsyncActionFilter
{
    private const string HeaderName    = "Idempotency-Key";
    private const string ReplayHeader  = "X-Idempotent-Replayed";
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rawKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

        // No header → pass-through
        if (rawKey is null)
        {
            await next();
            return;
        }

        // Validate UUIDv4 format
        if (!Guid.TryParse(rawKey, out var keyGuid))
        {
            context.Result = new BadRequestObjectResult(new { errorCode = "INVALID_IDEMPOTENCY_KEY" });
            return;
        }

        var cacheKey = $"idempotency:{keyGuid:N}";

        // Cache hit → replay
        var cached = await cache.GetAsync(cacheKey, context.HttpContext.RequestAborted);
        if (cached is not null)
        {
            var body = Encoding.UTF8.GetString(cached);
            logger.LogInformation("Idempotency replay for key {Key}", keyGuid);

            context.HttpContext.Response.Headers[ReplayHeader] = "true";
            context.Result = new ContentResult
            {
                Content     = body,
                ContentType = "application/json",
                StatusCode  = StatusCodes.Status200OK
            };
            return;
        }

        // Execute and cache successful non-5xx responses
        var executed = await next();

        if (executed.Result is ObjectResult { StatusCode: >= 200 and < 500 } objResult)
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(objResult.Value);
            await cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serialized), CacheOptions,
                context.HttpContext.RequestAborted);
        }
    }
}
