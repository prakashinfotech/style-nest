/**
 * ENH-ADMIN-003 — Scheduled Jobs abstraction.
 * Phase 9.8 deferred → implemented as IHostedService-based runners.
 *
 * Design: job logic lives in IScheduledJob implementations (plain services,
 * only depend on AppDbContext) so they are:
 *   1. testable independently of any scheduling framework
 *   2. triggerable on demand via AdminJobsController
 *   3. runnable on a background timer via JobSchedulerBackgroundService
 *
 * When ENH-ADMIN-002 (Hangfire) is implemented, each job is replaced by a
 * [AutomaticRetry]-annotated Hangfire job — the IScheduledJob.ExecuteAsync
 * method body moves verbatim into the Hangfire job method.
 */

namespace StyleNest.Admin.API.Services;

/// <summary>
/// Outcome returned by every scheduled job execution.
/// Captures what the job did and how long it took for logging and admin display.
/// </summary>
public sealed record JobResult(
    /// <summary>Stable, human-readable job identifier (e.g. "DailyAnalyticsJob").</summary>
    string   JobName,
    /// <summary>Number of database rows inserted, updated, or logically processed.</summary>
    int      ItemsAffected,
    /// <summary>UTC timestamp when this execution started.</summary>
    DateTime ExecutedAt,
    /// <summary>Wall-clock time the job took to complete.</summary>
    TimeSpan Duration);

/// <summary>
/// Contract for all scheduled background jobs.
/// Inject the concrete implementation to trigger a job on demand;
/// the <see cref="JobSchedulerBackgroundService"/> invokes implementations on a timer.
/// </summary>
public interface IScheduledJob
{
    /// <summary>Stable identifier used in admin endpoint routing and log messages.</summary>
    string JobName { get; }

    /// <summary>
    /// Runs the job's business logic.
    /// <paramref name="utcNow"/> is injected (not read from <see cref="DateTime.UtcNow"/>)
    /// so that tests can control the clock precisely.
    /// </summary>
    Task<JobResult> ExecuteAsync(DateTime utcNow, CancellationToken ct = default);
}
