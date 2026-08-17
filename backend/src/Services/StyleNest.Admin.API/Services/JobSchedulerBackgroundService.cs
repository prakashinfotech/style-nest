/**
 * ENH-ADMIN-003 — Background service that runs scheduled jobs on a PeriodicTimer.
 *
 * Design notes:
 * - Uses IServiceScopeFactory to resolve scoped AppDbContext per execution
 *   (BackgroundService is a singleton, but AppDbContext is scoped).
 * - Each job runs independently on its own interval; one job's failure does
 *   not block others.
 * - When ENH-ADMIN-002 (Hangfire) is implemented, this service is removed and
 *   jobs are enqueued via Hangfire's RecurringJob.AddOrUpdate().
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StyleNest.Admin.API.Services;

public sealed class JobSchedulerBackgroundService(
    IServiceScopeFactory               scopeFactory,
    ILogger<JobSchedulerBackgroundService> logger) : BackgroundService
{
    private static readonly (Type JobType, TimeSpan Interval)[] Jobs =
    [
        (typeof(DailyAnalyticsJob),  TimeSpan.FromHours(24)),
        (typeof(LowStockAlertJob),   TimeSpan.FromHours(6)),
        (typeof(CartAbandonmentJob), TimeSpan.FromHours(1)),
        (typeof(ExpireCouponsJob),   TimeSpan.FromHours(1)),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run all job loops concurrently; each manages its own PeriodicTimer.
        await Task.WhenAll(Jobs.Select(
            j => RunJobLoopAsync(j.JobType, j.Interval, stoppingToken)));
    }

    private async Task RunJobLoopAsync(Type jobType, TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            using var scope = scopeFactory.CreateScope();
            var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(jobType);

            try
            {
                var result = await job.ExecuteAsync(DateTime.UtcNow, ct);
                logger.LogInformation(
                    "[{Job}] completed — {ItemsAffected} item(s) in {Ms}ms",
                    result.JobName, result.ItemsAffected,
                    (int)result.Duration.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break; // clean shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{Job}] execution failed", jobType.Name);
                // Continue — next tick will retry the job
            }
        }
    }
}
