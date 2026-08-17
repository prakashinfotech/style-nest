/**
 * ENH-ADMIN-005 — Dynamic Attribute Filtering: facet generation service.
 *
 * Given a category (and optional active filters / search), returns all
 * filterable AttributeDefinitions for that category together with:
 *   - each distinct value found on at least one active product in that category
 *   - the count of matching products for each value
 *
 * This powers the left-hand filter sidebar on the PLP.
 *
 * Performance note:
 *   The query is O(attributes × distinct-values) groupBy operations which is
 *   acceptable for the attribute cardinalities expected (<100 filterable attrs,
 *   <50 distinct values each).  Heavier use cases would cache results with a
 *   10-minute Redis TTL (wired in via ICacheService at the controller level).
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Catalog.API.Services;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed record AttributeFacetValue(string Value, int Count);

public sealed record AttributeFacet(
    Guid                      AttributeId,
    string                    Name,
    string                    DisplayName,
    string                    DataType,
    IReadOnlyList<AttributeFacetValue> Values);

public sealed record AttributeFacetsResult(
    Guid                      CategoryId,
    IReadOnlyList<AttributeFacet> Facets);

// ─── Interface ───────────────────────────────────────────────────────────────

public interface IDynamicAttributeFilterService
{
    /// <summary>
    /// Returns filterable attribute facets (name + value + count) for all active products
    /// in <paramref name="categoryId"/>, optionally scoped to a brand or search term.
    /// </summary>
    Task<AttributeFacetsResult> GetFacetsAsync(
        Guid    categoryId,
        Guid?   brandId   = null,
        string? search    = null,
        CancellationToken ct = default);
}

// ─── Implementation ──────────────────────────────────────────────────────────

public sealed class DynamicAttributeFilterService(AppDbContext db) : IDynamicAttributeFilterService
{
    /// <inheritdoc />
    public async Task<AttributeFacetsResult> GetFacetsAsync(
        Guid    categoryId,
        Guid?   brandId   = null,
        string? search    = null,
        CancellationToken ct = default)
    {
        // 1. Determine which attributes are filterable for this category
        var filterableAttrIds = await db.CategoryAttributes
            .AsNoTracking()
            .Where(ca => ca.CategoryId == categoryId)
            .Select(ca => ca.AttributeDefinitionId)
            .ToListAsync(ct);

        if (filterableAttrIds.Count == 0)
            return new AttributeFacetsResult(categoryId, []);

        // 2. Build base product scope (active, in category, optional brand/search)
        var productQuery = db.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive);

        if (brandId.HasValue)
            productQuery = productQuery.Where(p => p.BrandId == brandId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            productQuery = productQuery.Where(p =>
                p.Name.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        var activeProductIds = await productQuery.Select(p => p.Id).ToListAsync(ct);
        if (activeProductIds.Count == 0)
            return new AttributeFacetsResult(categoryId, []);

        // 3. Fetch attribute definitions for this category
        var attrDefs = await db.AttributeDefinitions
            .AsNoTracking()
            .Where(a => filterableAttrIds.Contains(a.Id) && a.IsFilterable)
            .OrderBy(a => a.DisplayName)
            .ToListAsync(ct);

        if (attrDefs.Count == 0)
            return new AttributeFacetsResult(categoryId, []);

        // 4. Compute value counts: GROUP BY (AttributeDefinitionId, Value)
        var valueCounts = await db.ProductAttributes
            .AsNoTracking()
            .Where(pa =>
                filterableAttrIds.Contains(pa.AttributeDefinitionId) &&
                activeProductIds.Contains(pa.ProductId))
            .GroupBy(pa => new { pa.AttributeDefinitionId, pa.Value })
            .Select(g => new
            {
                g.Key.AttributeDefinitionId,
                g.Key.Value,
                Count = g.Count(),
            })
            .ToListAsync(ct);

        // 5. Build facets list
        var countLookup = valueCounts
            .GroupBy(x => x.AttributeDefinitionId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new AttributeFacetValue(x.Value, x.Count))
                       .OrderBy(v => v.Value)
                       .ToList());

        var facets = attrDefs
            .Where(a => countLookup.ContainsKey(a.Id))
            .Select(a => new AttributeFacet(
                a.Id,
                a.Name,
                a.DisplayName,
                a.DataType,
                countLookup[a.Id]))
            .ToList();

        return new AttributeFacetsResult(categoryId, facets);
    }
}
