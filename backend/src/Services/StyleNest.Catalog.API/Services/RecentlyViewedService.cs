/**
 * ENH-CAT-001 — Recently Viewed Products Rail (last 12 views per user)
 *
 * Functional specification (FR-HOME-006):
 *   • RecordViewAsync: one row per (UserId, ProductId) — upsert semantics.
 *     If the product was already viewed, ViewedAt is refreshed so it rises
 *     to the front of the list without creating a duplicate.
 *   • The rail is capped at MaxViews=12. After each upsert the oldest entries
 *     beyond the cap are pruned so the table never grows unbounded per user.
 *   • GetRecentlyViewedAsync: returns product DTOs ordered newest-first,
 *     joined to the Product table so only active, non-deleted products appear.
 *   • Guest/anonymous users: pass SessionId instead of UserId; scope is limited
 *     to the session rather than the user account.
 *
 * Acceptance criteria (TC-CAT-001-*):
 *   TC-CAT-001-01: RecordView inserts new entry for first view
 *   TC-CAT-001-02: RecordView on already-viewed product updates ViewedAt (upsert)
 *   TC-CAT-001-03: Cap enforced — 13th view triggers pruning to 12
 *   TC-CAT-001-04: Get returns entries ordered newest-first
 *   TC-CAT-001-05: Get respects limit parameter
 *   TC-CAT-001-06: Get cross-user isolation (User A sees only their views)
 *   TC-CAT-001-07: Inactive products excluded from Get result
 *   TC-CAT-001-08: Guest (sessionId) isolation — no cross-session contamination
 *   TC-CAT-001-09: RecordView for non-existent product does not throw
 *   TC-CAT-001-10: Re-viewing a product moves it to the front of the list
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Analytics;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-001 — A single item in the recently-viewed rail.</summary>
public sealed record RecentlyViewedItemDto(
    Guid     ProductId,
    string   Name,
    string   Slug,
    decimal  BasePrice,
    decimal? DiscountedPrice,
    DateTime ViewedAt);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IRecentlyViewedService
{
    /// <summary>
    /// ENH-CAT-001 — Records or refreshes a product view for the given user.
    /// Upserts the (UserId, ProductId) pair and enforces the MaxViews=12 cap.
    /// </summary>
    Task RecordViewAsync(
        Guid? userId, string? sessionId, Guid productId,
        DateTime? now = null, CancellationToken ct = default);

    /// <summary>
    /// ENH-CAT-001 — Returns the most recently viewed active products for the given user
    /// (or session), ordered newest-first and capped at <paramref name="limit"/>.
    /// </summary>
    Task<IReadOnlyList<RecentlyViewedItemDto>> GetRecentlyViewedAsync(
        Guid? userId, string? sessionId, int limit = 12, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-001 — Recently Viewed Products Rail.</summary>
public sealed class RecentlyViewedService(
    AppDbContext db,
    ILogger<RecentlyViewedService> logger) : IRecentlyViewedService
{
    /// <summary>Maximum number of recently-viewed entries retained per user/session.</summary>
    public const int MaxViews = 12;

    public async Task RecordViewAsync(
        Guid? userId, string? sessionId, Guid productId,
        DateTime? now = null, CancellationToken ct = default)
    {
        var viewedAt = now ?? DateTime.UtcNow;

        // ── Upsert ────────────────────────────────────────────────────────────
        // Find an existing view row for this (user/session, product) combination.
        var existing = await FindExistingAsync(userId, sessionId, productId, ct);

        if (existing is not null)
        {
            // Refresh the timestamp so the product rises to the front of the rail
            existing.ViewedAt = viewedAt;
            existing.UpdatedAt = viewedAt;
        }
        else
        {
            db.ProductViews.Add(new ProductView
            {
                Id        = Guid.NewGuid(),
                ProductId = productId,
                UserId    = userId,
                SessionId = sessionId,
                ViewedAt  = viewedAt,
                Source    = "rail",
                CreatedAt = viewedAt,
                UpdatedAt = viewedAt,
            });
        }

        await db.SaveChangesAsync(ct);

        // ── Cap enforcement ───────────────────────────────────────────────────
        // Count how many entries this user/session has.
        // If > MaxViews, delete the oldest (lowest ViewedAt) ones.
        var count = await CountUserViewsAsync(userId, sessionId, ct);

        if (count > MaxViews)
        {
            var toDelete = await db.ProductViews
                .Where(v => (userId.HasValue ? v.UserId == userId : v.UserId == null)
                         && (sessionId == null || v.SessionId == sessionId))
                .OrderBy(v => v.ViewedAt)
                .Take(count - MaxViews)
                .ToListAsync(ct);

            db.ProductViews.RemoveRange(toDelete);
            await db.SaveChangesAsync(ct);
        }

        logger.LogDebug(
            "{EventType} UserId={UserId} ProductId={ProductId}",
            existing is not null ? "RECENTLY_VIEWED_REFRESHED" : "RECENTLY_VIEWED_RECORDED",
            userId, productId);
    }

    public async Task<IReadOnlyList<RecentlyViewedItemDto>> GetRecentlyViewedAsync(
        Guid? userId, string? sessionId, int limit = 12, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxViews);

        // Join ProductViews → Products; exclude inactive / soft-deleted products
        var query =
            from pv in db.ProductViews
            join p  in db.Products on pv.ProductId equals p.Id
            where p.IsActive && !p.IsDeleted
            select new { pv, p };

        // Scope to this user or session
        if (userId.HasValue)
            query = query.Where(x => x.pv.UserId == userId);
        else if (sessionId is not null)
            query = query.Where(x => x.pv.SessionId == sessionId);
        else
            return [];   // anonymous with no session — nothing to show

        return await query
            .OrderByDescending(x => x.pv.ViewedAt)
            .Take(limit)
            .Select(x => new RecentlyViewedItemDto(
                x.p.Id,
                x.p.Name,
                x.p.Slug,
                x.p.BasePrice,
                x.p.DiscountedPrice,
                x.pv.ViewedAt))
            .ToListAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Task<ProductView?> FindExistingAsync(
        Guid? userId, string? sessionId, Guid productId, CancellationToken ct)
    {
        if (userId.HasValue)
            return db.ProductViews
                .FirstOrDefaultAsync(v => v.UserId == userId && v.ProductId == productId, ct);

        if (sessionId is not null)
            return db.ProductViews
                .FirstOrDefaultAsync(v => v.SessionId == sessionId && v.ProductId == productId, ct);

        return Task.FromResult<ProductView?>(null);
    }

    private Task<int> CountUserViewsAsync(
        Guid? userId, string? sessionId, CancellationToken ct)
    {
        if (userId.HasValue)
            return db.ProductViews.CountAsync(v => v.UserId == userId, ct);

        if (sessionId is not null)
            return db.ProductViews.CountAsync(v => v.SessionId == sessionId, ct);

        return Task.FromResult(0);
    }
}
