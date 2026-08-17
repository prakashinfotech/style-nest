/**
 * ENH-AI-001 — Personalised Product Feed (≥5 product views in 30d → 12-product rail)
 * ENH-AI-002 — Personalised Feed Fallback — trendingByCategory for guests / cold-start
 *
 * Acceptance criteria:
 *   ENH-AI-001:
 *     - userId present + ≥5 distinct product views in 30d → IsPersonalised=true, Reason="PERSONALISED"
 *     - Top 3 categories from view history used as personalisation signal
 *     - Already-viewed products excluded from the personalised rail
 *     - If personalised count < limit, fills remainder with trending products
 *   ENH-AI-002:
 *     - userId null → Reason="GUEST", falls back to trending
 *     - 0 views in 30d → Reason="COLD_START", falls back to trending
 *     - 1–4 views in 30d → Reason="INSUFFICIENT_VIEWS", falls back to trending
 *     - GetTrendingAsync filters by categoryId when provided
 *     - Trending window = 7 days; inactive products excluded
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>ENH-AI-001 — A single item in a personalised or trending product rail.</summary>
public sealed record ProductFeedItemDto(
    Guid     ProductId,
    string   Name,
    string   Slug,
    decimal  BasePrice,
    decimal? DiscountedPrice,
    Guid     CategoryId);

/// <summary>ENH-AI-001 — Full personalised feed response including fallback metadata.</summary>
public sealed record PersonalisedFeedResponse(
    bool                    IsPersonalised,
    string                  Reason,        // "PERSONALISED" | "INSUFFICIENT_VIEWS" | "COLD_START" | "GUEST"
    List<ProductFeedItemDto> Products);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IPersonalisedFeedService
{
    /// <summary>
    /// ENH-AI-001 — Returns up to <paramref name="limit"/> personalised products for the user.
    /// Eligibility: ≥5 distinct product views in the past 30 days.
    /// Falls back to <see cref="GetTrendingAsync"/> for guests, cold-start, or ineligible users.
    /// </summary>
    Task<PersonalisedFeedResponse> GetFeedAsync(
        Guid? userId, int limit = 12, CancellationToken ct = default);

    /// <summary>
    /// ENH-AI-002 — Returns the top <paramref name="limit"/> trending products
    /// (most viewed in the past 7 days), optionally filtered to a single category.
    /// </summary>
    Task<List<ProductFeedItemDto>> GetTrendingAsync(
        Guid? categoryId = null, int limit = 12, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class PersonalisedFeedService(
    AppDbContext db,
    ILogger<PersonalisedFeedService> logger) : IPersonalisedFeedService
{
    // ── Thresholds (constants for testability) ────────────────────────────────
    public const int MinViewsThreshold  = 5;
    public const int ViewWindowDays     = 30;
    public const int TrendingWindowDays = 7;
    public const int TopCategoriesCount = 3;

    // ── GetFeedAsync (ENH-AI-001) ─────────────────────────────────────────────

    public async Task<PersonalisedFeedResponse> GetFeedAsync(
        Guid? userId, int limit = 12, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        // ENH-AI-002: guest fallback
        if (userId is null)
        {
            logger.LogDebug("PersonalisedFeed: guest user — returning trending");
            return new PersonalisedFeedResponse(
                false, "GUEST", await GetTrendingProductsInternalAsync(null, limit, [], ct));
        }

        var windowStart = DateTime.UtcNow.AddDays(-ViewWindowDays);

        // Collect distinct product IDs viewed by this user in the eligibility window
        var viewedProductIds = await db.ProductViews
            .Where(v => v.UserId == userId && v.ViewedAt >= windowStart)
            .Select(v => v.ProductId)
            .Distinct()
            .ToListAsync(ct);

        // ENH-AI-002: cold-start or insufficient views fallback
        if (viewedProductIds.Count < MinViewsThreshold)
        {
            var reason = viewedProductIds.Count == 0 ? "COLD_START" : "INSUFFICIENT_VIEWS";

            logger.LogDebug(
                "PersonalisedFeed: UserId={UserId} ViewCount={Count} → {Reason}",
                userId, viewedProductIds.Count, reason);

            return new PersonalisedFeedResponse(
                false, reason, await GetTrendingProductsInternalAsync(null, limit, [], ct));
        }

        // Derive top categories from the user's view history
        var topCategories = await db.Products
            .Where(p => viewedProductIds.Contains(p.Id) && p.IsActive)
            .GroupBy(p => p.CategoryId)
            .OrderByDescending(g => g.Count())
            .Take(TopCategoriesCount)
            .Select(g => g.Key)
            .ToListAsync(ct);

        if (topCategories.Count == 0)
        {
            // All viewed products are inactive — cold-start
            return new PersonalisedFeedResponse(
                false, "COLD_START", await GetTrendingProductsInternalAsync(null, limit, [], ct));
        }

        // Personalised rail: products in top categories, never yet viewed by this user
        var personalised = await db.Products
            .Where(p => topCategories.Contains(p.CategoryId)
                     && p.IsActive
                     && !viewedProductIds.Contains(p.Id))
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .Take(limit)
            .Select(p => new ProductFeedItemDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice, p.CategoryId))
            .ToListAsync(ct);

        // Fill any remaining slots with trending (excluding what we already have + what user has seen)
        if (personalised.Count < limit)
        {
            var excludeIds = personalised
                .Select(p => p.ProductId)
                .Concat(viewedProductIds)
                .ToHashSet();

            var fill = await GetTrendingProductsInternalAsync(
                null, limit - personalised.Count, excludeIds, ct);

            personalised.AddRange(fill);
        }

        logger.LogInformation(
            "{EventType} UserId={UserId} ProductCount={Count} TopCategories={CategoryCount}",
            "PERSONALISED_FEED_SERVED", userId, personalised.Count, topCategories.Count);

        return new PersonalisedFeedResponse(true, "PERSONALISED", personalised);
    }

    // ── GetTrendingAsync (ENH-AI-002) ─────────────────────────────────────────

    public async Task<List<ProductFeedItemDto>> GetTrendingAsync(
        Guid? categoryId = null, int limit = 12, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        return await GetTrendingProductsInternalAsync(categoryId, limit, [], ct);
    }

    // ── Private: trending engine ──────────────────────────────────────────────

    private async Task<List<ProductFeedItemDto>> GetTrendingProductsInternalAsync(
        Guid? categoryId, int limit, HashSet<Guid> excludeIds, CancellationToken ct)
    {
        var trendingWindow = DateTime.UtcNow.AddDays(-TrendingWindowDays);

        // Rank productIds by view count in the trending window
        var viewCounts = await db.ProductViews
            .Where(v => v.ViewedAt >= trendingWindow)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit * 3)   // buffer: some candidates may be inactive or excluded
            .ToListAsync(ct);

        var candidateIds = viewCounts
            .Select(x => x.ProductId)
            .Except(excludeIds)
            .ToList();

        // Load active products from the ranked candidates
        var productQuery = db.Products
            .Where(p => p.IsActive && candidateIds.Contains(p.Id));

        if (categoryId.HasValue)
            productQuery = productQuery.Where(p => p.CategoryId == categoryId.Value);

        var products = await productQuery
            .Select(p => new ProductFeedItemDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice, p.CategoryId))
            .ToListAsync(ct);

        // Re-sort by trending rank (view count) and truncate to limit
        var viewCountMap = viewCounts.ToDictionary(x => x.ProductId, x => x.Count);
        return products
            .OrderByDescending(p => viewCountMap.TryGetValue(p.ProductId, out var vc) ? vc : 0)
            .Take(limit)
            .ToList();
    }
}
