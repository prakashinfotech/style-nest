using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Payments;

public enum PaymentStatus
{
    Initiated,
    Pending,
    Captured,
    Failed,
    Refunded,
    PartiallyRefunded
}

public enum PaymentMethod
{
    Card,
    NetBanking,
    Upi,
    Wallet,
    CashOnDelivery
}

public class Payment : BaseEntity<Guid>
{
    public Guid OrderId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? GatewayOrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public PaymentMethod Method { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}
