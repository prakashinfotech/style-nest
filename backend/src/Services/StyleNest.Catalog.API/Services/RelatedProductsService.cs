/**
 * ENH-PDP-005 — Related Products Rails: Similar, Complete the Look, FBT
 * ENH-AI-004  — AI-Powered Related Products: Frequently Bought Together
 *
 * Acceptance criteria:
 *   Similar:
 *     - Same CategoryId + same BrandId, different product, active, ordered by rating desc
 *     - Returns empty list for unknown productId
 *   Complete the Look:
 *     - Same CategoryId, different BrandId, active, ordered by ReviewCount desc
 *     - Excludes the source product and products in Similar rail
 *   FBT (Frequently Bought Together — ENH-AI-004):
 *     - Products purchased together with the source product in past orders
 *     - Ranked by co-occurrence count (distinct orders they share with source)
 *     - Returns empty list when no co-purchase history exists
 *   GetRelatedRailsAsync: returns all 3 rails in one response
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>ENH-PDP-005 — All three related-product rails for a PDP page.</summary>
public sealed record RelatedProductsRailsDto(
    List<ProductFeedItemDto> Similar,
    List<ProductFeedItemDto> CompleteTheLook,
    List<ProductFeedItemDto> FrequentlyBoughtTogether);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IRelatedProductsService
{
    /// <summary>ENH-PDP-005 — Returns Similar products (same category + brand, highest-rated first).</summary>
    Task<List<ProductFeedItemDto>> GetSimilarAsync(
        Guid productId, int limit = 8, CancellationToken ct = default);

    /// <summary>ENH-PDP-005 — Returns Complete the Look products (same category, different brand).</summary>
    Task<List<ProductFeedItemDto>> GetCompleteTheLookAsync(
        Guid productId, int limit = 8, CancellationToken ct = default);

    /// <summary>
    /// ENH-AI-004 — Returns Frequently Bought Together products
    /// ranked by order co-occurrence count.
    /// </summary>
    Task<List<ProductFeedItemDto>> GetFbtAsync(
        Guid productId, int limit = 8, CancellationToken ct = default);

    /// <summary>ENH-PDP-005/AI-004 — Returns all three rails in a single call.</summary>
    Task<RelatedProductsRailsDto> GetRelatedRailsAsync(
        Guid productId, int limit = 8, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class RelatedProductsService(
    AppDbContext db,
    ILogger<RelatedProductsService> logger) : IRelatedProductsService
{
    // ── Similar ───────────────────────────────────────────────────────────────

    public async Task<List<ProductFeedItemDto>> GetSimilarAsync(
        Guid productId, int limit = 8, CancellationToken ct = default)
    {
        var source = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId })
            .FirstOrDefaultAsync(ct);

        if (source is null) return [];

        return await db.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == source.CategoryId
                     && p.BrandId    == source.BrandId
                     && p.IsActive
                     && p.Id != productId)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.ReviewCount)
            .Take(limit)
            .Select(p => new ProductFeedItemDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice, p.CategoryId))
            .ToListAsync(ct);
    }

    // ── Complete the Look ─────────────────────────────────────────────────────

    public async Task<List<ProductFeedItemDto>> GetCompleteTheLookAsync(
        Guid productId, int limit = 8, CancellationToken ct = default)
    {
        var source = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.BrandId })
            .FirstOrDefaultAsync(ct);

        if (source is null) return [];

        return await db.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == source.CategoryId
                     && p.BrandId    != source.BrandId   // different brand = fresh perspective
                     && p.IsActive
                     && p.Id != productId)
            .OrderByDescending(p => p.ReviewCount)       // popular alternatives
            .ThenByDescending(p => p.AverageRating)
            .Take(limit)
            .Select(p => new ProductFeedItemDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice, p.CategoryId))
            .ToListAsync(ct);
    }

    // ── FBT (ENH-AI-004) ──────────────────────────────────────────────────────

    public async Task<List<ProductFeedItemDto>> GetFbtAsync(
        Guid productId, int limit = 8, CancellationToken ct = default)
    {
        // Step 1: ProductVariant IDs for the source product
        var sourceVariantIds = await db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .Select(v => v.Id)
            .ToListAsync(ct);

        if (sourceVariantIds.Count == 0) return [];

        // Step 2: Distinct Order IDs containing the source product
        var coOrderIds = await db.OrderItems
            .AsNoTracking()
            .Where(oi => sourceVariantIds.Contains(oi.ProductVariantId))
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync(ct);

        if (coOrderIds.Count == 0) return [];

        // Step 3: Other variant IDs in those orders (excluding source variants)
        var otherOrderItems = await db.OrderItems
            .AsNoTracking()
            .Where(oi => coOrderIds.Contains(oi.OrderId)
                      && !sourceVariantIds.Contains(oi.ProductVariantId))
            .Select(oi => new { oi.OrderId, oi.ProductVariantId })
            .ToListAsync(ct);

        if (otherOrderItems.Count == 0) return [];

        // Step 4: Map variant → product
        var otherVariantIds = otherOrderItems.Select(x => x.ProductVariantId).Distinct().ToList();

        var variantToProduct = await db.ProductVariants
            .AsNoTracking()
            .Where(v => otherVariantIds.Contains(v.Id))
            .Select(v => new { v.Id, v.ProductId })
            .ToDictionaryAsync(v => v.Id, v => v.ProductId, ct);

        // Step 5: Count distinct co-orders per product (FBT score)
        var fbtScores = otherOrderItems
            .Select(x => new
            {
                x.OrderId,
                ProductId = variantToProduct.TryGetValue(x.ProductVariantId, out var pid) ? pid : (Guid?)null,
            })
            .Where(x => x.ProductId.HasValue && x.ProductId.Value != productId)
            .GroupBy(x => x.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, Score = g.Select(x => x.OrderId).Distinct().Count() })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToDictionary(x => x.ProductId, x => x.Score);

        if (fbtScores.Count == 0) return [];

        var fbtProductIds = fbtScores.Keys.ToList();

        // Step 6: Load and sort active products by FBT score
        var products = await db.Products
            .AsNoTracking()
            .Where(p => fbtProductIds.Contains(p.Id) && p.IsActive && p.Id != productId)
            .Select(p => new ProductFeedItemDto(
                p.Id, p.Name, p.Slug, p.BasePrice, p.DiscountedPrice, p.CategoryId))
            .ToListAsync(ct);

        logger.LogInformation(
            "{EventType} ProductId={ProductId} FbtCount={Count}",
            "FBT_RAIL_SERVED", productId, products.Count);

        return products
            .OrderByDescending(p => fbtScores.TryGetValue(p.ProductId, out var s) ? s : 0)
            .ToList();
    }

    // ── Combined rails ────────────────────────────────────────────────────────

    public async Task<RelatedProductsRailsDto> GetRelatedRailsAsync(
        Guid productId, int limit = 8, CancellationToken ct = default)
    {
        var similar           = await GetSimilarAsync(productId, limit, ct);
        var completeTheLook   = await GetCompleteTheLookAsync(productId, limit, ct);
        var fbt               = await GetFbtAsync(productId, limit, ct);

        return new RelatedProductsRailsDto(similar, completeTheLook, fbt);
    }
}
