using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Seller;

public enum PayoutStatus { Pending, Processing, Completed, Failed }

public class SellerPayout : BaseEntity<Guid>
{
    public Guid SellerId { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
    public string? TransactionReference { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Notes { get; set; }

    public Seller Seller { get; set; } = null!;
}
