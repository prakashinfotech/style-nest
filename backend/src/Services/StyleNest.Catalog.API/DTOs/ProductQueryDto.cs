using System.ComponentModel.DataAnnotations;

namespace StyleNest.Catalog.API.DTOs;

public record ProductQueryDto
{
    public Guid?   CategoryId { get; init; }
    public Guid?   BrandId    { get; init; }
    public string? Search     { get; init; }
    public decimal? MinPrice  { get; init; }
    public decimal? MaxPrice  { get; init; }
    public string? Sort       { get; init; }

    /// <summary>Minimum discount percentage (1–99). Only products with DiscountedPrice set and
    /// discount &gt;= this value are returned.</summary>
    [Range(1, 99)]
    public int? MinDiscount { get; init; }

    [Range(1, int.MaxValue)]
    public int Page     { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 24;

    /// <summary>
    /// ENH-ADMIN-005 — EAV attribute filters.
    /// Key   = AttributeDefinition.Name  (e.g. "Color", "Material")
    /// Value = one or more allowed values for that attribute (OR within, AND across keys).
    /// Example: { "Color": ["Red", "Blue"], "Material": ["Cotton"] }
    ///   → products that have (Color = Red OR Blue) AND (Material = Cotton)
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? AttributeFilters { get; init; }
}
