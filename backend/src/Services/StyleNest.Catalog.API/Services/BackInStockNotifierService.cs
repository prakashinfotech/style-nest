using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

/// <summary>
/// ENH-PROMO-005 — Back-in-Stock Batch Notifier.
///
/// A hosted background service that runs on a configurable interval
/// (default: every 60 minutes, set via BackInStockNotifier:IntervalMinutes).
///
/// Each tick:
///   1. Queries BackInStockSubscriptions for products with un-notified, active subscribers.
///   2. Cross-references ProductVariants to confirm at least one variant is now in stock.
///   3. Calls BackInStockService.NotifySubscribersAsync for each eligible product, which
///      stamps NotifiedAt and enqueues NotificationOutbox entries (email + optional SMS).
///
/// The outbox entries are then dispatched by the notification delivery pipeline
/// (ENH-NOTIF-001 exponential-backoff retry). The notifier itself is idempotent:
/// subscriptions already stamped with NotifiedAt are skipped.
/// </summary>
public sealed class BackInStockNotifierService(
    IServiceScopeFactory scopeFactory,
    IConfiguration       config,
    ILogger<BackInStockNotifierService> logger) : BackgroundService
{
    private TimeSpan Interval => TimeSpan.FromMinutes(
        config.GetValue("BackInStockNotifier:IntervalMinutes", 60));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "ENH-PROMO-005 BackInStockNotifierService started. Interval={Interval}",
            Interval);

        // Run an initial batch immediately when the service starts so we don't miss
        // products that came back into stock before the first interval fires.
        await RunBatchAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunBatchAsync(stoppingToken);
        }
    }

    // ── Batch logic ──────────────────────────────────────────────────────────

    private async Task RunBatchAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IBackInStockService>();

            // ── Step 1: find product IDs with un-notified active subscriptions ──
            var productsWithPendingSubs = await db.BackInStockSubscriptions
                .Where(s => s.NotifiedAt == null && !s.IsDeleted)
                .Select(s => s.ProductId)
                .Distinct()
                .ToListAsync(ct);

            if (productsWithPendingSubs.Count == 0)
            {
                logger.LogDebug("ENH-PROMO-005 No pending back-in-stock subscriptions.");
                return;
            }

            // ── Step 2: intersect with products that have at least one in-stock variant ──
            // We only send notifications when stock is genuinely available to purchase.
            var productIdsToNotify = await db.ProductVariants
                .Where(v => productsWithPendingSubs.Contains(v.ProductId)
                            && v.StockQuantity > 0)
                .Select(v => v.ProductId)
                .Distinct()
                .ToListAsync(ct);

            if (productIdsToNotify.Count == 0)
            {
                logger.LogDebug(
                    "ENH-PROMO-005 {Pending} product(s) have pending subscriptions but none are currently in stock.",
                    productsWithPendingSubs.Count);
                return;
            }

            logger.LogInformation(
                "ENH-PROMO-005 Back-in-stock batch started. EligibleProducts={Count}",
                productIdsToNotify.Count);

            // ── Step 3: dispatch notifications ────────────────────────────────
            var totalNotified = 0;
            foreach (var productId in productIdsToNotify)
            {
                if (ct.IsCancellationRequested) break;

                var result = await svc.NotifySubscribersAsync(productId, ct);
                totalNotified += result.SubscribersNotified;

                if (result.SubscribersNotified > 0)
                    logger.LogInformation(
                        "ENH-PROMO-005 ProductId={ProductId} — {Count} subscriber(s) notified.",
                        productId, result.SubscribersNotified);
            }

            logger.LogInformation(
                "ENH-PROMO-005 Batch complete. ProductsProcessed={Products} TotalNotified={Total}",
                productIdsToNotify.Count, totalNotified);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful shutdown — not an error; the timer loop will exit cleanly.
            logger.LogInformation("ENH-PROMO-005 Batch cancelled (application shutting down).");
        }
        catch (Exception ex)
        {
            // Catch-all: log and continue. We must not let a single failed batch crash
            // the hosted service — it will retry on the next tick.
            logger.LogError(ex, "ENH-PROMO-005 BackInStockNotifierService batch failed.");
        }
    }
}
