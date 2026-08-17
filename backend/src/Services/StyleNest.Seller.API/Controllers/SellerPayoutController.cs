using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Seller.API.DTOs;
using StyleNest.Seller.API.Services;

namespace StyleNest.Seller.API.Controllers;

/// <summary>
/// ENH-SELL-003 — Seller Payout endpoints.
///
/// POST /api/v1/sellers/{sellerId}/payouts — Admin triggers a bank payout via Razorpay.
/// GET  /api/v1/sellers/{sellerId}/payouts — Returns payout history (Admin or Seller).
/// </summary>
[ApiController]
[Route("api/v1/sellers/{sellerId:guid}/payouts")]
[Authorize]
public sealed class SellerPayoutController(
    ISellerPayoutService payoutService,
    ILogger<SellerPayoutController> logger) : ControllerBase
{
    /// <summary>
    /// Triggers an automated bank payout for the specified seller via Razorpay.
    /// Restricted to Admin / SuperAdmin roles.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(PayoutResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerPayout(
        Guid                 sellerId,
        [FromBody] TriggerPayoutRequest request,
        CancellationToken    ct)
    {
        if (request.Amount <= 0)
            return BadRequest(new { code = "INVALID_AMOUNT", message = "Amount must be greater than zero." });

        try
        {
            var result = await payoutService.TriggerPayoutAsync(sellerId, request.Amount, request.Notes, ct);
            logger.LogInformation(
                "ENH-SELL-003: Payout triggered by admin for seller {SellerId} — ₹{Amount}",
                sellerId, request.Amount);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = "SELLER_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { code = "PAYOUT_PRECONDITION_FAILED", message = ex.Message });
        }
    }

    /// <summary>
    /// Returns payout history for the specified seller.
    /// Accessible to Admin/SuperAdmin or the seller themselves.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PayoutResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayouts(Guid sellerId, CancellationToken ct)
    {
        try
        {
            var payouts = await payoutService.GetPayoutsAsync(sellerId, ct);
            return Ok(payouts);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { code = "SELLER_NOT_FOUND", message = ex.Message });
        }
    }
}
