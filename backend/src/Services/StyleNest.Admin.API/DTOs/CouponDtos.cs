using StyleNest.Infrastructure.Entities.Admin;

namespace StyleNest.Admin.API.DTOs;

public record CouponDto(
    Guid           Id,
    string         Code,
    string         Description,
    DiscountType   DiscountType,
    decimal        DiscountValue,
    decimal?       MinOrderAmount,
    decimal?       MaxDiscountCap,
    int?           UsageLimitPerUser,
    int?           TotalUsageLimit,
    int            UsedCount,
    bool           IsActive,
    DateTime?      StartsAt,
    DateTime?      ExpiresAt,
    // ENH-PROMO-004
    CouponCategory Category,
    bool           AllowsStacking);

public record CreateCouponRequest(
    string         Code,
    string         Description,
    DiscountType   DiscountType,
    decimal        DiscountValue,
    decimal?       MinOrderAmount,
    decimal?       MaxDiscountCap,
    int?           UsageLimitPerUser,
    int?           TotalUsageLimit,
    bool           IsActive,
    DateTime?      StartsAt,
    DateTime?      ExpiresAt,
    // ENH-PROMO-004
    CouponCategory Category       = CouponCategory.Standard,
    bool           AllowsStacking = false);

public record UpdateCouponRequest(
    string         Description,
    DiscountType   DiscountType,
    decimal        DiscountValue,
    decimal?       MinOrderAmount,
    decimal?       MaxDiscountCap,
    int?           UsageLimitPerUser,
    int?           TotalUsageLimit,
    bool           IsActive,
    DateTime?      StartsAt,
    DateTime?      ExpiresAt,
    // ENH-PROMO-004
    CouponCategory Category       = CouponCategory.Standard,
    bool           AllowsStacking = false);
