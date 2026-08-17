/**
 * ENH-PROMO-004 — Coupon Stacking Rules validation endpoint.
 *
 * POST api/v1/admin/coupons/validate-stack
 *   Body: { "codes": ["SAVE10", "FREESHIP"] }
 *   → 200 OK  { valid: true }
 *   → 422     { valid: false, reason: "..." }
 *
 * Called by the cart/checkout service before applying multiple coupons.
 * Admin & storefront may also call it to surface UI feedback before order placement.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/coupons")]
[Authorize]
public sealed class CouponStackingController(ICouponStackingService stackingService) : ControllerBase
{
    public sealed record ValidateStackRequest(IReadOnlyList<string> Codes);
    public sealed record ValidateStackResponse(bool Valid, string? Reason = null);

    /// <summary>
    /// ENH-PROMO-004 — Validates whether the supplied coupon codes may be
    /// stacked on a single order according to the platform stacking rules.
    /// </summary>
    [HttpPost("validate-stack")]
    public async Task<IActionResult> ValidateStack(
        [FromBody] ValidateStackRequest request,
        CancellationToken ct)
    {
        if (request.Codes is null || request.Codes.Count == 0)
            return Ok(new ValidateStackResponse(true));

        try
        {
            await stackingService.ValidateCouponCodesAsync(request.Codes, ct);
            return Ok(new ValidateStackResponse(true));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ValidateStackResponse(false, ex.Message));
        }
    }
}
