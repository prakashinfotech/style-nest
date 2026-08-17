using System.ComponentModel.DataAnnotations;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class ProductVariant : BaseEntity<Guid>
{
    public Guid ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal? PriceOverride { get; set; }

    /// <summary>
    /// EF Core optimistic concurrency token — SQL Server rowversion.
    /// Prevents lost-update race conditions during concurrent checkout (EC-INV-001).
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public Product Product { get; set; } = null!;
    public ICollection<ProductVariantOption> Options { get; set; } = [];
}
