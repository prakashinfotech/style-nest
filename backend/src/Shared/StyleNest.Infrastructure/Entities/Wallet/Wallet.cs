using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Wallet;

public class Wallet : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; } = 0m;
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// ENH-PROMO-002 — Timestamp of the most recent debit purchase/redemption.
    /// Null = the user has never made a purchase using StyleNest Cash.
    /// Used by the 12-month inactivity expiry policy.
    /// </summary>
    public DateTime? LastPurchaseAt { get; set; }

    public ICollection<WalletTransaction> Transactions { get; set; } = [];
}
