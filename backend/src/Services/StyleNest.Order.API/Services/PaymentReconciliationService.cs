using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

// ── Job (scoped, testable) ────────────────────────────────────────────────────

public interface IPaymentReconciliationJob
{
    Task<int> RunAsync(CancellationToken ct = default);
}

/// <summary>
/// ENH-PAY-005 — Surfaces Initiated payments that fall inside the bank-timeout
/// window (T+60s → T+15min) as <see cref="PaymentStatus.Pending"/>.
/// </summary>
public sealed class PaymentReconciliationJob(
    AppDbContext db,
    ILogger<PaymentReconciliationJob> logger) : IPaymentReconciliationJob
{
    public static readonly TimeSpan MinAge = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15);

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var now        = DateTime.UtcNow;
        var windowEnd  = now - MinAge;   // older than 60s
        var windowStart = now - MaxAge;  // no older than 15min

        var timedOut = await db.Payments
            .Where(p => p.Status == PaymentStatus.Initiated
                     && p.CreatedAt >= windowStart
                     && p.CreatedAt <= windowEnd)
            .ToListAsync(ct);

        if (timedOut.Count == 0)
            return 0;

        foreach (var payment in timedOut)
        {
            payment.Status = PaymentStatus.Pending;
            logger.LogWarning(
                "PaymentReconciliation: payment {PaymentId} order {OrderId} " +
                "timed out at T+{AgeSeconds}s — surfaced as Pending for reconciliation",
                payment.Id, payment.OrderId,
                (int)(now - payment.CreatedAt).TotalSeconds);
        }

        await db.SaveChangesAsync(ct);
        return timedOut.Count;
    }
}

// ── BackgroundService (infrastructure) ───────────────────────────────────────

/// <summary>
/// Polls every 60 s and delegates to <see cref="IPaymentReconciliationJob"/>.
/// Uses a DI scope per tick so AppDbContext is not shared across iterations.
/// </summary>
public sealed class PaymentReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentReconciliationBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PaymentReconciliation service started (interval={Interval}s)",
            PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollInterval, stoppingToken);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var job   = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationJob>();
                var count = await job.RunAsync(stoppingToken);
                if (count > 0)
                    logger.LogInformation(
                        "PaymentReconciliation: surfaced {Count} timed-out payment(s) as Pending", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PaymentReconciliation: unhandled error during poll tick");
            }
        }
    }
}
