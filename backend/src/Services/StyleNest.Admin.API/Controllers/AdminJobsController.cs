/**
 * ENH-ADMIN-003 — Admin endpoint to manually trigger scheduled jobs on demand.
 *
 * POST api/v1/admin/jobs/{jobName}/run
 *
 * Valid job names:
 *   daily-analytics    → DailyAnalyticsJob
 *   low-stock-alert    → LowStockAlertJob
 *   cart-abandonment   → CartAbandonmentJob
 *   expire-coupons     → ExpireCouponsJob
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/jobs")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class AdminJobsController(
    DailyAnalyticsJob  dailyAnalytics,
    LowStockAlertJob   lowStockAlert,
    CartAbandonmentJob cartAbandonment,
    ExpireCouponsJob   expireCoupons) : ControllerBase
{
    private readonly IReadOnlyDictionary<string, IScheduledJob> _jobs =
        new Dictionary<string, IScheduledJob>(StringComparer.OrdinalIgnoreCase)
        {
            ["daily-analytics"]  = dailyAnalytics,
            ["low-stock-alert"]  = lowStockAlert,
            ["cart-abandonment"] = cartAbandonment,
            ["expire-coupons"]   = expireCoupons,
        };

    /// <summary>
    /// ENH-ADMIN-003 — Immediately runs the specified scheduled job and returns the result.
    /// Use for ops intervention, backfilling, or manual testing.
    /// </summary>
    [HttpPost("{jobName}/run")]
    public async Task<IActionResult> RunJob(string jobName, CancellationToken ct)
    {
        if (!_jobs.TryGetValue(jobName, out var job))
        {
            return NotFound(new
            {
                message      = $"Unknown job '{jobName}'.",
                validJobNames = _jobs.Keys.ToArray(),
            });
        }

        var result = await job.ExecuteAsync(DateTime.UtcNow, ct);
        return Ok(result);
    }

    /// <summary>
    /// ENH-ADMIN-003 — Lists all registered scheduled jobs and their identifiers.
    /// </summary>
    [HttpGet]
    public IActionResult ListJobs() =>
        Ok(_jobs.Select(kv => new { slug = kv.Key, name = kv.Value.JobName }));
}
