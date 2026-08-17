/**
 * ENH-ADMIN-007 — Distributed Tracing: W3C Trace Context propagation
 * Acceptance criteria:
 *   - Every inbound request receives a valid W3C traceparent header in the response
 *   - Incoming traceparent is parsed; child span inherits the trace ID
 *   - Invalid / missing traceparent generates a new root trace
 *   - tracestate is echoed in the response when supplied by the caller
 *   - Serilog LogContext is enriched with TraceId + SpanId for every log line in the request
 *   - Activity.DefaultIdFormat is forced to W3C on every request
 */

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace StyleNest.SharedKernel.Middleware;

/// <summary>
/// ENH-ADMIN-007 — W3C Trace Context middleware. Ensures every service participates
/// in distributed traces by reading the incoming <c>traceparent</c> / <c>tracestate</c>
/// headers, creating a new root span when no parent is present, and echoing the resolved
/// <c>traceparent</c> back in the response. All Serilog log lines emitted during the
/// request are enriched with <c>TraceId</c> and <c>SpanId</c>.
/// </summary>
public sealed class W3CTracingMiddleware(RequestDelegate next)
{
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader  = "tracestate";

    /// <summary>W3C traceparent format pattern: version-traceId-parentId-flags</summary>
    public static readonly System.Text.RegularExpressions.Regex TraceParentRegex =
        new(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
            System.Text.RegularExpressions.RegexOptions.Compiled |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        // Force W3C format for all Activities in this service process
        Activity.DefaultIdFormat      = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        // ASP.NET Core's DiagnosticListener starts an Activity for each request and
        // propagates the incoming traceparent header automatically. We start our own
        // span only if the host did not (e.g. in test harnesses or minimal hosts).
        Activity? owned = null;
        if (Activity.Current is null)
        {
            owned = new Activity("http.server.request");
            var incomingTp = context.Request.Headers[TraceParentHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(incomingTp) && TraceParentRegex.IsMatch(incomingTp))
                owned.SetParentId(incomingTp);

            var incomingTs = context.Request.Headers[TraceStateHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(incomingTs))
                owned.TraceStateString = incomingTs;

            owned.Start();
        }

        try
        {
            var activity = Activity.Current;

            // Echo the resolved traceparent in the response so clients can continue
            // the trace chain (e.g. browser, mobile app, partner integrations).
            context.Response.OnStarting(() =>
            {
                if (Activity.Current is { } cur)
                {
                    var flags = cur.Recorded ? "01" : "00";
                    context.Response.Headers[TraceParentHeader] =
                        $"00-{cur.TraceId}-{cur.SpanId}-{flags}";

                    if (!string.IsNullOrEmpty(cur.TraceStateString))
                        context.Response.Headers[TraceStateHeader] = cur.TraceStateString;
                }
                return Task.CompletedTask;
            });

            var traceId = activity?.TraceId.ToString() ?? new string('0', 32);
            var spanId  = activity?.SpanId.ToString()  ?? new string('0', 16);

            // Every Serilog log line emitted during this request will carry TraceId + SpanId,
            // enabling log-to-trace correlation in Application Insights / Seq / ELK.
            using (LogContext.PushProperty("TraceId", traceId))
            using (LogContext.PushProperty("SpanId",  spanId))
            {
                await next(context);
            }
        }
        finally
        {
            owned?.Stop();
            owned?.Dispose();
        }
    }
}
