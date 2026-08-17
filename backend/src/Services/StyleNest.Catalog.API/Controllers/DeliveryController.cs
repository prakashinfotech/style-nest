using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/delivery")]
public sealed class DeliveryController(IPincodeService pincodeService) : ControllerBase
{
    /// <summary>
    /// ENH-PDP-001 — Returns delivery estimate for a given pincode.
    /// Always returns HTTP 200; non-serviceable pincodes have <c>serviceable: false</c>.
    /// Unknown pincodes return degraded defaults (serviceable: true, etaDays: 5).
    /// </summary>
    /// <param name="pincode">6-digit Indian postal code.</param>
    /// <param name="productId">Optional product identifier (reserved for SKU-level overrides).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("estimate")]
    public async Task<IActionResult> GetEstimate(
        [FromQuery] string pincode,
        [FromQuery] Guid? productId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pincode) || pincode.Length != 6 || !pincode.All(char.IsDigit))
            return BadRequest(new { message = "pincode must be exactly 6 digits." });

        var estimate = await pincodeService.GetEstimateAsync(pincode, ct);

        return Ok(new
        {
            pincode,
            serviceable           = estimate.Serviceable,
            codEligible           = estimate.CodEligible,
            etaDays               = estimate.EtaDays,
            expressAvailable      = estimate.ExpressAvailable,
            freeDeliveryThreshold = estimate.FreeDeliveryThreshold,
            city                  = estimate.City,
        });
    }
}
