/**
 * ENH-ADMIN-003 — Scheduled Job implementations.
 *
 * Four jobs, each implementing IScheduledJob:
 *
 *   DailyAnalyticsJob   — aggregates yesterday's order revenue into analytics.DailyRevenue
 *   LowStockAlertJob    — counts seller inventory items below their LowStockThreshold
 *   CartAbandonmentJob  — counts carts idle > 24 h that still contain items
 *   ExpireCouponsJob    — deactivates coupons whose ExpiresAt has passed
 *
 * Clock: each job accepts a utcNow DateTime parameter rather than reading
 * DateTime.UtcNow directly, keeping the logic deterministic in unit tests.
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Admin.API.Services;

// ── 1. DailyAnalyticsJob ─────────────────────────────────────────────────────

/// <summary>
/// ENH-ADMIN-003 — Aggregates the previous day's completed orders into the
/// <c>analytics.DailyRevenue</c> table. Idempotent: re-running for the same
/// date updates the existing row rather than creating a duplicate.
/// </summary>
public sealed class DailyAnalyticsJob(AppDbContext db) : IScheduledJob
{
    public string JobName => "DailyAnalyticsJob";

    public async Task<JobResult> ExecuteAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var started   = utcNow;
        var yesterday = utcNow.Date.AddDays(-1);
        var dayEnd    = yesterday.AddDays(1);   // exclusive upper bound

        var orders = await db.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.CreatedAt >= yesterday &&
                        o.CreatedAt <  dayEnd    &&
                        o.Status    != OrderStatus.Cancelled)
            .ToListAsync(ct);

        var revenue        = orders.Sum(o => o.TotalAmount);
        var orderCount     = orders.Count;
        var itemCount      = orders.Sum(o => o.Items.Count);
        var avgOrderValue  = orderCount > 0 ? revenue / orderCount : 0m;

        // ── Upsert ────────────────────────────────────────────────────────────
        var existing = await db.DailyRevenues
            .FirstOrDefaultAsync(d => d.Date == yesterday, ct);

        if (existing is null)
        {
            db.DailyRevenues.Add(new DailyRevenue
            {
                Id                = Guid.NewGuid(),
                Date              = yesterday,
                Revenue           = revenue,
                OrderCount        = orderCount,
                ItemCount         = itemCount,
                AverageOrderValue = avgOrderValue,
                CreatedAt         = utcNow,
                UpdatedAt         = utcNow,
            });
        }
        else
        {
            existing.Revenue           = revenue;
            existing.OrderCount        = orderCount;
            existing.ItemCount         = itemCount;
            existing.AverageOrderValue = avgOrderValue;
            existing.UpdatedAt         = utcNow;
        }

        await db.SaveChangesAsync(ct);

        return new JobResult(JobName, 1, started, utcNow - started);
    }
}

// ── 2. LowStockAlertJob ──────────────────────────────────────────────────────

/// <summary>
/// ENH-ADMIN-003 — Scans <c>seller.SellerInventory</c> for items that are
/// below their individual <see cref="StyleNest.Infrastructure.Entities.Seller.SellerInventory.LowStockThreshold"/>
/// but still have some stock (Stock &gt; 0). Returns the count of affected items
/// so the caller can emit alerts / notifications.
/// </summary>
public sealed class LowStockAlertJob(AppDbContext db) : IScheduledJob
{
    public string JobName => "LowStockAlertJob";

    public async Task<JobResult> ExecuteAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var started = utcNow;

        var lowStockCount = await db.SellerInventories
            .AsNoTracking()
            .Where(i => !i.IsDeleted &&
                        i.Stock > 0  &&
                        i.Stock <= i.LowStockThreshold)
            .CountAsync(ct);

        return new JobResult(JobName, lowStockCount, started, utcNow - started);
    }
}

// ── 3. CartAbandonmentJob ────────────────────────────────────────────────────

/// <summary>
/// ENH-ADMIN-003 — Identifies carts that contain items but have not been
/// updated for more than <see cref="AbandonmentThresholdHours"/> hours.
/// Returns the count of such abandoned carts. In a full implementation this
/// count drives email/push re-engagement campaigns.
/// </summary>
public sealed class CartAbandonmentJob(AppDbContext db) : IScheduledJob
{
    public string JobName => "CartAbandonmentJob";

    /// <summary>A cart is considered abandoned after this many hours of inactivity.</summary>
    public const int AbandonmentThresholdHours = 24;

    public async Task<JobResult> ExecuteAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var started = utcNow;
        var cutoff  = utcNow.AddHours(-AbandonmentThresholdHours);

        // Resolve cart IDs that have at least one item — avoids navigation-based
        // Any() in WHERE which can be unreliable on the InMemory provider.
        var cartIdsWithItems = await db.CartItems
            .AsNoTracking()
            .Where(ci => !ci.IsDeleted)
            .Select(ci => ci.CartId)
            .Distinct()
            .ToListAsync(ct);

        var abandonedCount = cartIdsWithItems.Count == 0
            ? 0
            : await db.Carts
                .AsNoTracking()
                .Where(c => !c.IsDeleted &&
                            c.UpdatedAt < cutoff &&
                            cartIdsWithItems.Contains(c.Id))
                .CountAsync(ct);

        return new JobResult(JobName, abandonedCount, started, utcNow - started);
    }
}

// ── 4. ExpireCouponsJob ──────────────────────────────────────────────────────

/// <summary>
/// ENH-ADMIN-003 — Deactivates all coupons whose <c>ExpiresAt</c> timestamp
/// is in the past and that are still marked <c>IsActive = true</c>.
/// Returns the count of coupons deactivated.
/// </summary>
public sealed class ExpireCouponsJob(AppDbContext db) : IScheduledJob
{
    public string JobName => "ExpireCouponsJob";

    public async Task<JobResult> ExecuteAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var started = utcNow;

        // Note: Coupon has a global query filter (!IsDeleted), so deleted coupons
        // are already excluded — no need to add !IsDeleted here.
        var expired = await db.Coupons
            .Where(c => c.IsActive && c.ExpiresAt.HasValue && c.ExpiresAt <= utcNow)
            .ToListAsync(ct);

        foreach (var coupon in expired)
        {
            coupon.IsActive   = false;
            coupon.UpdatedAt  = utcNow;
        }

        if (expired.Count > 0)
            await db.SaveChangesAsync(ct);

        return new JobResult(JobName, expired.Count, started, utcNow - started);
    }
}
