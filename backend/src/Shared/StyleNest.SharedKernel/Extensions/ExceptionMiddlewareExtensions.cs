using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using StyleNest.SharedKernel.Middleware;

namespace StyleNest.SharedKernel.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();

    /// <summary>
    /// ENH-ADMIN-007 — Registers the <see cref="W3CTracingMiddleware"/> and forces
    /// <see cref="Activity.DefaultIdFormat"/> to <see cref="ActivityIdFormat.W3C"/> so every
    /// <see cref="Activity"/> in this process uses the 32-hex TraceId / 16-hex SpanId format.
    /// Call this early in the pipeline (before Serilog request logging) so all log lines
    /// in the request scope are enriched with TraceId + SpanId.
    /// </summary>
    public static IApplicationBuilder UseW3CTracing(this IApplicationBuilder app)
    {
        // Set process-wide defaults once at startup (re-entry is idempotent).
        Activity.DefaultIdFormat      = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
        return app.UseMiddleware<W3CTracingMiddleware>();
    }
}
