/**
 * ENH-CAT-002 — Flash Sale Module: server-driven countdown + sold-out transition
 * Schema: catalog.FlashSales + catalog.FlashSaleItems
 */

using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public enum FlashSaleStatus
{
    Scheduled = 0,
    Active    = 1,
    Ended     = 2,
}

/// <summary>ENH-CAT-002 — A timed flash-sale event that discounts a set of products.</summary>
public class FlashSale : BaseEntity<Guid>
{
    public string Name     { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt   { get; set; }

    /// <summary>Managed by the server: Scheduled → Active when StartsAt reached; Active → Ended when EndsAt passed.</summary>
    public FlashSaleStatus Status { get; set; } = FlashSaleStatus.Scheduled;

    public ICollection<FlashSaleItem> Items { get; set; } = [];
}

/// <summary>ENH-CAT-002 — A single product entry within a FlashSale with its own price + stock cap.</summary>
public class FlashSaleItem : BaseEntity<Guid>
{
    public Guid FlashSaleId   { get; set; }
    public Guid ProductId     { get; set; }

    /// <summary>Discounted price charged during the flash sale.</summary>
    public decimal SalePrice     { get; set; }

    /// <summary>Original (pre-sale) price shown as the struck-through reference.</summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>Maximum units available at the flash-sale price. 0 = unlimited.</summary>
    public int StockLimit { get; set; }

    /// <summary>Units sold so far at the flash-sale price. Incremented by order placement.</summary>
    public int SoldCount  { get; set; }

    /// <summary>True once SoldCount ≥ StockLimit (and StockLimit > 0). Set by server — never client.</summary>
    public bool IsSoldOut { get; set; }

    public FlashSale FlashSale { get; set; } = null!;
    public Product   Product   { get; set; } = null!;
}
