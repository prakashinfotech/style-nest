using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Wallet;

public enum TransactionType { Credit, Debit }
public enum TransactionSource
{
    OrderRefund, ManualTopup, CashbackReward, OrderPayment, Adjustment, Redemption,
    /// <summary>ENH-PROMO-002 — StyleNest Cash expiry debit (12-month inactivity).</summary>
    Expiry
}

public class WalletTransaction : BaseEntity<Guid>
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionSource Source { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal BalanceAfter { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
