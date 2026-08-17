/**
 * ENH-CAT-007 — SEO Canonicalisation
 * Acceptance criteria:
 *   - GetProductSeoAsync: checks SeoMetadata override first; falls back to template
 *   - GetCategorySeoAsync: checks SeoMetadata override first; falls back to template
 *   - Partial override: any null override field falls back to the template value
 *   - HasCustomOverride=true only when a SeoMetadata row exists for the entity
 *   - Unknown productId / categoryId → returns null
 *   - Canonical path auto-generated: /products/{slug} for products, /category/{slug} for categories
 *   - Title template: "{Name} — Buy Online at StyleNest" (product)
 *                     "Buy {Name} Online at Best Prices | StyleNest" (category)
 *   - Description template: derived from entity name + brand (product) or name (category)
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ── Response ──────────────────────────────────────────────────────────────────

/// <summary>ENH-CAT-007 — SEO meta values for a single page.</summary>
public sealed record SeoMetaResponse(
    string PageTitle,
    string MetaDescription,

    /// <summary>Relative canonical path (e.g. "/products/nike-air-max-90").</summary>
    string CanonicalPath,

    /// <summary>True when at least one field came from a stored <c>SeoMetadata</c> override.</summary>
    bool HasCustomOverride);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface ISeoService
{
    /// <summary>
    /// ENH-CAT-007 — Returns SEO metadata for a product.
    /// Uses stored overrides where present; fills remaining fields from auto-generated templates.
    /// Returns <see langword="null"/> when the product does not exist.
    /// </summary>
    Task<SeoMetaResponse?> GetProductSeoAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// ENH-CAT-007 — Returns SEO metadata for a category.
    /// Uses stored overrides where present; fills remaining fields from auto-generated templates.
    /// Returns <see langword="null"/> when the category does not exist.
    /// </summary>
    Task<SeoMetaResponse?> GetCategorySeoAsync(Guid categoryId, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class SeoService(AppDbContext db) : ISeoService
{
    private const string EntityTypeProduct  = "product";
    private const string EntityTypeCategory = "category";

    // ── Product SEO ───────────────────────────────────────────────────────────

    public async Task<SeoMetaResponse?> GetProductSeoAsync(
        Guid productId, CancellationToken ct = default)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null) return null;

        // Check for stored override
        var override_ = await db.SeoMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.EntityType == EntityTypeProduct && s.EntityId == productId, ct);

        // Auto-generated templates
        var autoTitle       = $"{product.Name} — Buy Online at StyleNest";
        var brandName       = product.Brand?.Name ?? "Top Brands";
        var autoDescription = $"Shop {product.Name} from {brandName}. Get the best price and fast delivery at StyleNest.";
        var autoCanonical   = $"/products/{product.Slug}";

        var hasOverride = override_ is not null;

        return new SeoMetaResponse(
            PageTitle:       override_?.TitleOverride           ?? autoTitle,
            MetaDescription: override_?.MetaDescriptionOverride ?? autoDescription,
            CanonicalPath:   override_?.CanonicalPathOverride   ?? autoCanonical,
            HasCustomOverride: hasOverride);
    }

    // ── Category SEO ──────────────────────────────────────────────────────────

    public async Task<SeoMetaResponse?> GetCategorySeoAsync(
        Guid categoryId, CancellationToken ct = default)
    {
        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null) return null;

        // Check for stored override
        var override_ = await db.SeoMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.EntityType == EntityTypeCategory && s.EntityId == categoryId, ct);

        // Auto-generated templates
        var autoTitle       = $"Buy {category.Name} Online at Best Prices | StyleNest";
        var autoDescription = $"Explore the widest range of {category.Name} products online. Shop with confidence at StyleNest.";
        var autoCanonical   = $"/category/{category.Slug}";

        var hasOverride = override_ is not null;

        return new SeoMetaResponse(
            PageTitle:       override_?.TitleOverride           ?? autoTitle,
            MetaDescription: override_?.MetaDescriptionOverride ?? autoDescription,
            CanonicalPath:   override_?.CanonicalPathOverride   ?? autoCanonical,
            HasCustomOverride: hasOverride);
    }
}
