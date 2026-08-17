using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Orders;

public class OrderStatusHistory : BaseEntity<Guid>
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = null!;
}
