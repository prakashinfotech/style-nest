/**
 * ENH-PROMO-004 — Coupon Stacking Rules.
 *
 * Business rules (from SOW v2.1 FR-CART-006 g):
 *   1. A maximum of 2 coupons may be applied per order.
 *   2. Both coupons must have AllowsStacking = true.
 *   3. The two coupons must belong to DIFFERENT CouponCategory values.
 *      (e.g. Standard + FreeShipping is valid; Standard + Standard is not.)
 *   4. Zero or one coupon is always valid regardless of stacking flags.
 */

using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StyleNest.Admin.API.Services;

public interface ICouponStackingService
{
    /// <summary>
    /// Returns true when coupon <paramref name="a"/> and <paramref name="b"/>
    /// may be combined according to the stacking rules.
    /// </summary>
    bool CanStack(Coupon a, Coupon b);

    /// <summary>
    /// Validates a set of coupon codes for stacking eligibility.
    /// Throws <see cref="InvalidOperationException"/> if any rule is violated.
    /// </summary>
    Task ValidateCouponCodesAsync(IReadOnlyList<string> codes, CancellationToken ct = default);
}

public sealed class CouponStackingService(AppDbContext db) : ICouponStackingService
{
    /// <inheritdoc />
    public bool CanStack(Coupon a, Coupon b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Both must opt-in
        if (!a.AllowsStacking || !b.AllowsStacking)
            return false;

        // Same category not allowed (avoids doubling the same discount type)
        if (a.Category == b.Category)
            return false;

        return true;
    }

    /// <inheritdoc />
    public async Task ValidateCouponCodesAsync(IReadOnlyList<string> codes, CancellationToken ct = default)
    {
        if (codes.Count == 0 || codes.Count == 1)
            return; // 0 or 1 coupon always valid

        if (codes.Count > 2)
            throw new InvalidOperationException(
                $"A maximum of 2 coupons may be applied per order. Received {codes.Count}.");

        // Exactly 2 — fetch both and validate
        var coupons = await db.Coupons
            .AsNoTracking()
            .Where(c => codes.Contains(c.Code))
            .ToListAsync(ct);

        // Ensure both codes exist
        var missing = codes.Except(coupons.Select(c => c.Code), StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Coupon code(s) not found: {string.Join(", ", missing)}.");

        var first  = coupons[0];
        var second = coupons[1];

        if (!CanStack(first, second))
        {
            var reason = (!first.AllowsStacking || !second.AllowsStacking)
                ? $"Coupon '{(first.AllowsStacking ? second.Code : first.Code)}' does not allow stacking."
                : $"Coupons '{first.Code}' and '{second.Code}' belong to the same category ({first.Category}) and cannot be combined.";

            throw new InvalidOperationException(
                $"These coupons cannot be combined. {reason}");
        }
    }
}
