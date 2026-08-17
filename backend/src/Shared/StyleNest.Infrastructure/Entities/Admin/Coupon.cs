using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Admin;

public enum DiscountType
{
    Percentage,
    FlatAmount
}

/// <summary>
/// ENH-PROMO-004 — Categorises a coupon so stacking rules can distinguish
/// coupon types.  Two coupons of the same category cannot stack even if both
/// have AllowsStacking = true.
/// </summary>
public enum CouponCategory
{
    Standard      = 0,
    FreeShipping  = 1,
    WelcomeOffer  = 2,
    LoyaltyReward = 3,
    FlashSale     = 4,
}

public class Coupon : BaseEntity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountCap { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // ENH-PROMO-004 — Stacking support
    /// <summary>Coupon category used to enforce cross-category stacking rule.</summary>
    public CouponCategory Category { get; set; } = CouponCategory.Standard;

    /// <summary>
    /// When true this coupon may be combined with one other coupon that also
    /// has AllowsStacking = true AND belongs to a different category.
    /// </summary>
    public bool AllowsStacking { get; set; } = false;
}
