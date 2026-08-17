/**
 * ENH-CHKOUT-002 — Express Checkout controller
 *
 * GET  api/v1/checkout/express          → eligibility preview (default address + card)
 * POST api/v1/checkout/express          → one-tap order placement
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.Exceptions;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

[ApiController]
[Route("api/v1/checkout/express")]
[Authorize]
public sealed class ExpressCheckoutController(
    IExpressCheckoutService expressCheckout) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// ENH-CHKOUT-002 — Returns the pre-filled default address and payment card
    /// that will be used for one-tap checkout, plus an eligibility flag.
    /// The client MUST call this before showing the "Express Checkout" button so it
    /// can surface the correct block reason when the user is not eligible.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExpressPreview(CancellationToken ct)
    {
        var preview = await expressCheckout.GetPreviewAsync(UserId, ct);
        return Ok(preview);
    }

    /// <summary>
    /// ENH-CHKOUT-002 — Places an order using the user's saved default address and
    /// default card token. No address/payment form data is required.
    /// An optional coupon code may be sent in the JSON body.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceExpressOrder(
        [FromBody] ExpressCheckoutBodyRequest? body,
        CancellationToken ct)
    {
        try
        {
            var order = await expressCheckout.PlaceExpressOrderAsync(
                UserId, body?.CouponCode, ct);

            return Created($"/api/v1/orders/{order.Id}", order);
        }
        catch (CheckoutEmailUnverifiedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                errorCode      = "CHECKOUT_EMAIL_UNVERIFIED",
                message        = ex.Message,
                verifyEmailUrl = "/api/v1/auth/email/verify/send",
            });
        }
        // ConcurrentCheckoutException and InventoryValidationException extend InvalidOperationException;
        // they must be caught BEFORE the base catch.
        catch (ConcurrentCheckoutException ex)
        {
            return Conflict(new { errorCode = "CHECKOUT_CONFLICT", message = ex.Message });
        }
        catch (InventoryValidationException ex)
        {
            return UnprocessableEntity(new
            {
                errorCode = "INVENTORY_VALIDATION_FAILED",
                message   = ex.Message,
                outOfStockItems = ex.OutOfStockItems.Select(i => new
                {
                    variantId         = i.VariantId,
                    productName       = i.ProductName,
                    variantDetails    = i.VariantDetails,
                    requestedQuantity = i.RequestedQuantity,
                    availableQuantity = i.AvailableQuantity,
                }),
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>Optional request body for express checkout — only a coupon code is accepted.</summary>
public sealed record ExpressCheckoutBodyRequest(string? CouponCode);
