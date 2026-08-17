using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

[ApiController]
[Route("api/v1/checkout")]
[Authorize]
public sealed class CheckoutController(
    IPaymentOptionsService paymentOptions,
    IPaymentGatewaySelector gatewaySelector) : ControllerBase
{
    /// <summary>
    /// Returns available payment methods for a given cart total.
    /// ENH-CHKOUT-003 — COD excluded when cartTotal &gt; ₹50,000 (TC-PAY-BVA-002).
    /// ENH-PAY-001 — Active card/netbanking gateway reflects Razorpay/PayU failover selection.
    /// </summary>
    [HttpGet("payment-options")]
    public async Task<IActionResult> GetPaymentOptions(
        [FromQuery] decimal cartTotal,
        CancellationToken ct)
    {
        if (cartTotal < 0)
            return BadRequest(new { message = "cartTotal must be non-negative." });

        // ENH-PAY-001: select primary or failover gateway
        var activeGateway = await gatewaySelector.SelectAsync(ct);

        var options = paymentOptions.GetOptions(cartTotal);

        // Replace "RAZORPAY" slot with whichever gateway is currently active
        var result = options
            .Select(o => o.Method == "RAZORPAY" ? o with { Method = activeGateway } : o)
            .ToList();

        return Ok(result);
    }
}
