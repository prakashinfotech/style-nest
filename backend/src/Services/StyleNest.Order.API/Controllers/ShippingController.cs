using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Order.API.Services;

namespace StyleNest.Order.API.Controllers;

// ── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>ENH-ORD-005 — NDR webhook payload from Shiprocket / Delhivery.</summary>
public sealed record NdrWebhookRequest(
    Guid    OrderId,
    string  NdrReason,
    string? Location);

/// <summary>ENH-ORD-005 — Carrier tracking event push payload.</summary>
public sealed record TrackingEventWebhookRequest(
    Guid     OrderId,
    string   EventType,
    string   Description,
    string?  Location,
    DateTime? OccurredAt);

// ── Controller ────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/v1")]
public sealed class ShippingController(IShippingService shippingService) : ControllerBase
{
    /// <summary>
    /// ENH-ORD-005 — Returns shipment tracking events for an order.
    /// </summary>
    [HttpGet("orders/{orderId:guid}/tracking")]
    [Authorize]
    public async Task<IActionResult> GetTracking(Guid orderId, CancellationToken ct)
    {
        var tracking = await shippingService.GetTrackingAsync(orderId, ct);
        if (tracking is null)
            return NotFound(new { message = $"Order {orderId} not found." });

        return Ok(tracking);
    }

    /// <summary>
    /// ENH-ORD-005 — Internal endpoint: creates a shipment (assigns AWB) for an order
    /// when it transitions to Shipped. Called by Order.API orchestration logic.
    /// Restricted to service-to-service (no public auth required in dev).
    /// </summary>
    [HttpPost("orders/{orderId:guid}/shipment")]
    [Authorize(Roles = "Admin,SuperAdmin,Seller")]
    public async Task<IActionResult> CreateShipment(Guid orderId, CancellationToken ct)
    {
        await shippingService.CreateShipmentAsync(orderId, ct);
        return Ok(new { message = "Shipment created." });
    }

    /// <summary>
    /// ENH-ORD-005 — Carrier webhook: Non-Delivery Report.
    /// Called by Shiprocket / Delhivery when a delivery attempt fails.
    /// No auth header required (carrier webhook); validated by shared secret in production.
    /// </summary>
    [HttpPost("webhooks/shipping/ndr")]
    [AllowAnonymous]
    public async Task<IActionResult> NdrWebhook(
        [FromBody] NdrWebhookRequest req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(req.NdrReason))
            return BadRequest(new { message = "OrderId and NdrReason are required." });

        await shippingService.HandleNdrAsync(req.OrderId, req.NdrReason, req.Location, ct);
        return Ok(new { message = "NDR recorded." });
    }

    /// <summary>
    /// ENH-ORD-005 — Carrier webhook: generic tracking event push.
    /// </summary>
    [HttpPost("webhooks/shipping/event")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackingEventWebhook(
        [FromBody] TrackingEventWebhookRequest req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(req.EventType))
            return BadRequest(new { message = "OrderId and EventType are required." });

        await shippingService.AddTrackingEventAsync(
            req.OrderId, req.EventType, req.Description, req.Location, req.OccurredAt, ct);

        return Ok(new { message = "Tracking event recorded." });
    }
}
