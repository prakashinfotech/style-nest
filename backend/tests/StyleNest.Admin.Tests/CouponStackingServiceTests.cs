/**
 * ENH-PROMO-004 — Coupon Stacking Rules: CouponStackingService
 *
 * Acceptance criteria (SOW v2.1 FR-CART-006 g):
 *   TC-PROMO-004-01: CanStack — both AllowsStacking=true, different categories → true
 *   TC-PROMO-004-02: CanStack — first AllowsStacking=false → false
 *   TC-PROMO-004-03: CanStack — second AllowsStacking=false → false
 *   TC-PROMO-004-04: CanStack — both AllowsStacking=false → false
 *   TC-PROMO-004-05: CanStack — both true but same category → false
 *   TC-PROMO-004-06: CanStack — Standard + FreeShipping both true → true
 *   TC-PROMO-004-07: CanStack — LoyaltyReward + FlashSale both true → true
 *   TC-PROMO-004-08: ValidateCouponCodesAsync — 0 codes → valid, no throw
 *   TC-PROMO-004-09: ValidateCouponCodesAsync — 1 code → valid, no throw
 *   TC-PROMO-004-10: ValidateCouponCodesAsync — 3 codes → throws (>2 limit)
 *   TC-PROMO-004-11: ValidateCouponCodesAsync — 2 codes, non-stackable → throws
 *   TC-PROMO-004-12: ValidateCouponCodesAsync — 2 codes, same category, both true → throws
 *   TC-PROMO-004-13: ValidateCouponCodesAsync — 2 valid stackable codes → no throw
 *   TC-PROMO-004-14: ValidateCouponCodesAsync — unknown code → throws with meaningful message
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Admin.API.Services;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Admin.Tests;

public sealed class CouponStackingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CouponStackingService _svc;

    public CouponStackingServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
        _svc = new CouponStackingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── CanStack (pure logic, no DB) ────────────────────────────────────────

    [Fact(DisplayName = "TC-PROMO-004-01: CanStack — both true, different categories → true")]
    public void CanStack_BothTrue_DifferentCategories_ReturnsTrue()
    {
        var a = MakeCoupon("A", CouponCategory.Standard,     allowsStacking: true);
        var b = MakeCoupon("B", CouponCategory.FreeShipping, allowsStacking: true);

        _svc.CanStack(a, b).Should().BeTrue();
    }

    [Fact(DisplayName = "TC-PROMO-004-02: CanStack — first AllowsStacking=false → false")]
    public void CanStack_FirstFalse_ReturnsFalse()
    {
        var a = MakeCoupon("A", CouponCategory.Standard,     allowsStacking: false);
        var b = MakeCoupon("B", CouponCategory.FreeShipping, allowsStacking: true);

        _svc.CanStack(a, b).Should().BeFalse();
    }

    [Fact(DisplayName = "TC-PROMO-004-03: CanStack — second AllowsStacking=false → false")]
    public void CanStack_SecondFalse_ReturnsFalse()
    {
        var a = MakeCoupon("A", CouponCategory.Standard,     allowsStacking: true);
        var b = MakeCoupon("B", CouponCategory.FreeShipping, allowsStacking: false);

        _svc.CanStack(a, b).Should().BeFalse();
    }

    [Fact(DisplayName = "TC-PROMO-004-04: CanStack — both AllowsStacking=false → false")]
    public void CanStack_BothFalse_ReturnsFalse()
    {
        var a = MakeCoupon("A", CouponCategory.Standard,     allowsStacking: false);
        var b = MakeCoupon("B", CouponCategory.FreeShipping, allowsStacking: false);

        _svc.CanStack(a, b).Should().BeFalse();
    }

    [Fact(DisplayName = "TC-PROMO-004-05: CanStack — both true but same category → false")]
    public void CanStack_BothTrue_SameCategory_ReturnsFalse()
    {
        var a = MakeCoupon("A", CouponCategory.Standard, allowsStacking: true);
        var b = MakeCoupon("B", CouponCategory.Standard, allowsStacking: true);

        _svc.CanStack(a, b).Should().BeFalse();
    }

    [Fact(DisplayName = "TC-PROMO-004-06: CanStack — Standard + FreeShipping both true → true")]
    public void CanStack_Standard_FreeShipping_ReturnsTrue()
    {
        var a = MakeCoupon("SAVE10",   CouponCategory.Standard,     allowsStacking: true);
        var b = MakeCoupon("FREESHIP", CouponCategory.FreeShipping, allowsStacking: true);

        _svc.CanStack(a, b).Should().BeTrue();
    }

    [Fact(DisplayName = "TC-PROMO-004-07: CanStack — LoyaltyReward + FlashSale both true → true")]
    public void CanStack_Loyalty_FlashSale_ReturnsTrue()
    {
        var a = MakeCoupon("LOYALTY20", CouponCategory.LoyaltyReward, allowsStacking: true);
        var b = MakeCoupon("FLASH50",   CouponCategory.FlashSale,     allowsStacking: true);

        _svc.CanStack(a, b).Should().BeTrue();
    }

    // ─── ValidateCouponCodesAsync (DB-backed) ────────────────────────────────

    [Fact(DisplayName = "TC-PROMO-004-08: ValidateCouponCodesAsync — 0 codes → no throw")]
    public async Task Validate_ZeroCodes_DoesNotThrow()
    {
        var act = async () => await _svc.ValidateCouponCodesAsync([]);

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "TC-PROMO-004-09: ValidateCouponCodesAsync — 1 code → no throw")]
    public async Task Validate_OneCode_DoesNotThrow()
    {
        await SeedCouponAsync("SINGLE10", CouponCategory.Standard, allowsStacking: false);

        var act = async () => await _svc.ValidateCouponCodesAsync(["SINGLE10"]);

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "TC-PROMO-004-10: ValidateCouponCodesAsync — 3 codes → throws exceeds limit")]
    public async Task Validate_ThreeCodes_ThrowsExceedsLimit()
    {
        var act = async () => await _svc.ValidateCouponCodesAsync(["A", "B", "C"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum of 2*");
    }

    [Fact(DisplayName = "TC-PROMO-004-11: ValidateCouponCodesAsync — 2 codes, first non-stackable → throws")]
    public async Task Validate_TwoCodes_FirstNonStackable_Throws()
    {
        await SeedCouponAsync("NOSAVE",   CouponCategory.Standard,     allowsStacking: false);
        await SeedCouponAsync("FREESHIP", CouponCategory.FreeShipping, allowsStacking: true);

        var act = async () => await _svc.ValidateCouponCodesAsync(["NOSAVE", "FREESHIP"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be combined*");
    }

    [Fact(DisplayName = "TC-PROMO-004-12: ValidateCouponCodesAsync — 2 codes, same category, both true → throws")]
    public async Task Validate_TwoCodes_SameCategory_Throws()
    {
        await SeedCouponAsync("STD1", CouponCategory.Standard, allowsStacking: true);
        await SeedCouponAsync("STD2", CouponCategory.Standard, allowsStacking: true);

        var act = async () => await _svc.ValidateCouponCodesAsync(["STD1", "STD2"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be combined*");
    }

    [Fact(DisplayName = "TC-PROMO-004-13: ValidateCouponCodesAsync — 2 valid stackable codes → no throw")]
    public async Task Validate_TwoValidStackable_DoesNotThrow()
    {
        await SeedCouponAsync("SAVE10",   CouponCategory.Standard,     allowsStacking: true);
        await SeedCouponAsync("FREESHIP", CouponCategory.FreeShipping, allowsStacking: true);

        var act = async () => await _svc.ValidateCouponCodesAsync(["SAVE10", "FREESHIP"]);

        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "TC-PROMO-004-14: ValidateCouponCodesAsync — unknown code → throws with code name")]
    public async Task Validate_UnknownCode_ThrowsWithCodeName()
    {
        await SeedCouponAsync("REAL10", CouponCategory.Standard, allowsStacking: true);

        var act = async () => await _svc.ValidateCouponCodesAsync(["REAL10", "GHOST99"]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*GHOST99*");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Coupon MakeCoupon(string code, CouponCategory category, bool allowsStacking) =>
        new()
        {
            Id             = Guid.NewGuid(),
            Code           = code,
            Description    = code,
            DiscountType   = DiscountType.Percentage,
            DiscountValue  = 10m,
            IsActive       = true,
            Category       = category,
            AllowsStacking = allowsStacking,
        };

    private async Task SeedCouponAsync(string code, CouponCategory category, bool allowsStacking)
    {
        _db.Coupons.Add(MakeCoupon(code, category, allowsStacking));
        await _db.SaveChangesAsync();
    }
}
