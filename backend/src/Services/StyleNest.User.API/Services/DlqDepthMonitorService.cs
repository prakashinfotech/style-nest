using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.User.API.Services;

/// <summary>
/// ENH-NOTIF-005 — DLQ Depth Alert.
///
/// Monitors the <see cref="NotificationOutbox"/> for <see cref="OutboxStatus.DeadLettered"/>
/// records and raises a structured alert when:
///   depth  >  <see cref="DlqDepthThreshold"/> (100 messages)   AND
///   the depth has been elevated for at least <see cref="ElevatedDurationThreshold"/> (15 min)
///
/// The alert is emitted as a structured <see cref="LogLevel.Critical"/> entry so that
/// Azure Application Insights / Azure Monitor can fire a configured alert rule on it
/// (filter: customDimensions.EventId == 5001).
///
/// The monitor also emits a <see cref="LogLevel.Information"/> recovery entry when
/// the depth falls back below the threshold so the alert condition can auto-resolve.
/// </summary>
public interface IDlqDepthMonitor
{
    /// <summary>Returns the count of DLQ entries created within the retention window.</summary>
    Task<int> GetCurrentDepthAsync(CancellationToken ct = default);
}

// ── Scoped job (testable) ─────────────────────────────────────────────────────

public sealed class DlqDepthMonitorJob(
    AppDbContext db,
    ILogger<DlqDepthMonitorJob> logger) : IDlqDepthMonitor
{
    /// <summary>Only count dead-lettered records within the last 7 days (DLQ retention window).</summary>
    private static readonly TimeSpan DlqRetentionWindow = TimeSpan.FromDays(7);

    public async Task<int> GetCurrentDepthAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow - DlqRetentionWindow;
        var depth = await db.NotificationOutbox
            .Where(n => n.Status == OutboxStatus.DeadLettered && n.CreatedAt >= since)
            .CountAsync(ct);

        logger.LogDebug("ENH-NOTIF-005: DLQ depth = {Depth} (window: last 7 days)", depth);
        return depth;
    }
}

// ── Singleton state tracker ───────────────────────────────────────────────────

/// <summary>
/// Singleton that tracks when the DLQ depth first exceeded the threshold so
/// the background service can measure the 15-minute sustained elevation.
/// </summary>
public sealed class DlqAlertState
{
    private readonly object _lock = new();
    private DateTime? _elevatedSince;
    private bool _alertFired;

    /// <summary>
    /// Call on every poll tick with the current depth.
    /// Returns the action to take: <c>FireAlert</c>, <c>RecoverAlert</c>, or <c>None</c>.
    /// </summary>
    public DlqAlertAction Update(int depth, int threshold, TimeSpan durationThreshold)
    {
        lock (_lock)
        {
            if (depth > threshold)
            {
                _elevatedSince ??= DateTime.UtcNow;

                var elevated = DateTime.UtcNow - _elevatedSince.Value;
                if (!_alertFired && elevated >= durationThreshold)
                {
                    _alertFired = true;
                    return DlqAlertAction.FireAlert;
                }
            }
            else
            {
                if (_elevatedSince.HasValue)
                {
                    // Depth dropped back below threshold
                    var wasElevated = _alertFired;
                    _elevatedSince = null;
                    _alertFired    = false;
                    return wasElevated ? DlqAlertAction.RecoverAlert : DlqAlertAction.None;
                }
            }

            return DlqAlertAction.None;
        }
    }
}

public enum DlqAlertAction { None, FireAlert, RecoverAlert }

// ── BackgroundService ─────────────────────────────────────────────────────────

/// <summary>
/// ENH-NOTIF-005 — Hosted service that polls DLQ depth every 5 minutes and
/// emits a structured Critical log when depth > 100 for > 15 minutes.
/// </summary>
public sealed class DlqDepthMonitorBackgroundService(
    IServiceScopeFactory scopeFactory,
    DlqAlertState        alertState,
    ILogger<DlqDepthMonitorBackgroundService> logger) : BackgroundService
{
    // ── Thresholds (FR-NOTIF-006) ─────────────────────────────────────────────
    internal const int DlqDepthThreshold = 100;
    internal static readonly TimeSpan ElevatedDurationThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PollInterval              = TimeSpan.FromMinutes(5);

    // Structured event IDs for Azure Monitor alert rules
    private static readonly EventId AlertEventId    = new(5001, "DlqDepthAlertFired");
    private static readonly EventId RecoverEventId  = new(5002, "DlqDepthAlertRecovered");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "ENH-NOTIF-005: DLQ depth monitor started — threshold={Threshold} messages " +
            "sustained for {Duration} min.",
            DlqDepthThreshold, ElevatedDurationThreshold.TotalMinutes);

        // Initial delay so the service starts cleanly before first poll
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ENH-NOTIF-005: Unhandled error during DLQ depth poll tick.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var monitor = scope.ServiceProvider.GetRequiredService<IDlqDepthMonitor>();
        var depth   = await monitor.GetCurrentDepthAsync(ct);

        var action  = alertState.Update(depth, DlqDepthThreshold, ElevatedDurationThreshold);

        switch (action)
        {
            case DlqAlertAction.FireAlert:
                // Critical log — App Insights routes this to an Azure Monitor Alert Rule
                // Query: customDimensions.EventId == "5001" severity == "Critical"
                logger.LogCritical(AlertEventId,
                    "ENH-NOTIF-005: DLQ DEPTH ALERT — {Depth} dead-lettered notifications " +
                    "have been accumulating for >{Duration} minutes. " +
                    "Investigate the notification retry pipeline immediately. " +
                    "EventId={EventId}",
                    depth, ElevatedDurationThreshold.TotalMinutes, AlertEventId.Id);
                break;

            case DlqAlertAction.RecoverAlert:
                logger.LogInformation(RecoverEventId,
                    "ENH-NOTIF-005: DLQ DEPTH RECOVERED — depth {Depth} is now below threshold {Threshold}. " +
                    "EventId={EventId}",
                    depth, DlqDepthThreshold, RecoverEventId.Id);
                break;

            case DlqAlertAction.None:
            default:
                break;
        }
    }
}
