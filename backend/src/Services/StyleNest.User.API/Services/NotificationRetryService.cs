using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.User.API.Services;

// ── Abstractions ──────────────────────────────────────────────────────────────

public interface INotificationSender
{
    Task<bool> SendAsync(NotificationOutbox item, CancellationToken ct = default);
}

/// <summary>Dev/test no-op sender — always reports success after logging.</summary>
public sealed class NullNotificationSender(ILogger<NullNotificationSender> logger) : INotificationSender
{
    public Task<bool> SendAsync(NotificationOutbox item, CancellationToken ct = default)
    {
        logger.LogInformation(
            "NullSender: would send {Type} to user {UserId} — subject: {Subject}",
            item.Type, item.UserId, item.Subject);
        return Task.FromResult(true);
    }
}

public interface INotificationDlqSink
{
    Task EnqueueAsync(NotificationOutbox item, CancellationToken ct = default);
}

/// <summary>Dev/test sink — logs DLQ entry; Azure Service Bus wires in for production.</summary>
public sealed class NullNotificationDlqSink(ILogger<NullNotificationDlqSink> logger) : INotificationDlqSink
{
    public Task EnqueueAsync(NotificationOutbox item, CancellationToken ct = default)
    {
        logger.LogWarning(
            "DLQ: notification {Id} type={Type} userId={UserId} exhausted all retries — dead-lettered",
            item.Id, item.Type, item.UserId);
        return Task.CompletedTask;
    }
}

// ── Job (scoped, testable) ────────────────────────────────────────────────────

public interface INotificationRetryJob
{
    Task<int> RunAsync(CancellationToken ct = default);
}

/// <summary>
/// ENH-NOTIF-001 — Processes pending outbox notifications with exponential-backoff
/// retry: 60s → 180s → 540s (1m, 3m, 9m). After 3 retries the record is dead-lettered
/// via <see cref="INotificationDlqSink"/> (Azure Service Bus in production).
/// </summary>
public sealed class NotificationRetryJob(
    AppDbContext db,
    INotificationSender sender,
    INotificationDlqSink dlq,
    ILogger<NotificationRetryJob> logger) : INotificationRetryJob
{
    // 3^0 * 60, 3^1 * 60, 3^2 * 60 seconds → 1m, 3m, 9m
    public static readonly int[] RetryDelaysSeconds = [60, 180, 540];
    public const int MaxAttempts = 4; // 1 initial attempt + 3 retries

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var due = await db.NotificationOutbox
            .Where(n => n.Status == OutboxStatus.Pending
                     && (n.NextAttemptAt == null || n.NextAttemptAt <= now))
            .ToListAsync(ct);

        if (due.Count == 0) return 0;

        var processed = 0;
        foreach (var item in due)
        {
            item.AttemptCount++;

            bool sent;
            try
            {
                sent = await sender.SendAsync(item, ct);
            }
            catch (Exception ex)
            {
                sent = false;
                item.LastError = ex.Message;
                logger.LogWarning(ex, "NotificationRetry: send failed for {Id}", item.Id);
            }

            if (sent)
            {
                item.Status = OutboxStatus.Delivered;
                item.LastError = null;
                logger.LogInformation(
                    "NotificationRetry: delivered {Id} type={Type} attempt={Attempt}",
                    item.Id, item.Type, item.AttemptCount);
            }
            else if (item.AttemptCount >= MaxAttempts)
            {
                item.Status = OutboxStatus.DeadLettered;
                await dlq.EnqueueAsync(item, ct);
                logger.LogWarning(
                    "NotificationRetry: dead-lettered {Id} after {Attempts} attempts",
                    item.Id, item.AttemptCount);
            }
            else
            {
                var delaySecs = RetryDelaysSeconds[item.AttemptCount - 1];
                item.NextAttemptAt = now.AddSeconds(delaySecs);
                item.Status = OutboxStatus.Pending;
                logger.LogInformation(
                    "NotificationRetry: scheduled retry #{Attempt} for {Id} in {Delay}s",
                    item.AttemptCount, item.Id, delaySecs);
            }

            processed++;
        }

        await db.SaveChangesAsync(ct);
        return processed;
    }
}

// ── BackgroundService (infrastructure) ───────────────────────────────────────

public sealed class NotificationRetryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationRetryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationRetry background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollInterval, stoppingToken);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job   = scope.ServiceProvider.GetRequiredService<INotificationRetryJob>();
                var count = await job.RunAsync(stoppingToken);
                if (count > 0)
                    logger.LogInformation("NotificationRetry: processed {Count} outbox item(s)", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "NotificationRetry: unhandled error during poll tick");
            }
        }
    }
}
