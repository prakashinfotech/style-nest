using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Seller;

public class SellerInventory : BaseEntity<Guid>
{
    public Guid SellerId { get; set; }
    public Guid ProductVariantId { get; set; }
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    public Seller Seller { get; set; } = null!;
}
