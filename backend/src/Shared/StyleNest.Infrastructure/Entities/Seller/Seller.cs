using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Seller;

public enum SellerStatus { Pending, Active, Suspended, Rejected }

public class Seller : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? GstNumber { get; set; }
    public string? PanNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIfsc { get; set; }
    public SellerStatus Status { get; set; } = SellerStatus.Pending;
    public decimal CommissionRate { get; set; } = 15.0m;
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public ICollection<SellerInventory> Inventory { get; set; } = [];
    public ICollection<SellerPayout> Payouts { get; set; } = [];
}
