using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Orders;

public class OrderItem : BaseEntity<Guid>
{
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantDetails { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
