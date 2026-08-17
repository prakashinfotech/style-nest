/**
 * ENH-PDP-006 — Back-in-Stock Notification endpoints.
 *
 * POST   api/v1/products/{productId}/back-in-stock        → subscribe [Authorize]
 * DELETE api/v1/products/{productId}/back-in-stock        → unsubscribe [Authorize]
 * GET    api/v1/products/{productId}/back-in-stock        → current subscription status [Authorize]
 * POST   api/v1/products/{productId}/back-in-stock/notify → trigger batch notify [Admin]
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/back-in-stock")]
public sealed class BackInStockController(IBackInStockService svc) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>ENH-PDP-006 — Subscribe to back-in-stock notification.</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Subscribe(
        Guid productId,
        [FromBody] SubscribeBackInStockRequest request,
        CancellationToken ct = default)
    {
        var dto = await svc.SubscribeAsync(productId, CurrentUserId, request, ct);
        return Ok(dto);
    }

    /// <summary>ENH-PDP-006 — Unsubscribe from back-in-stock notification.</summary>
    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Unsubscribe(
        Guid productId,
        [FromQuery] Guid? variantId = null,
        CancellationToken ct = default)
    {
        await svc.UnsubscribeAsync(productId, CurrentUserId, variantId, ct);
        return NoContent();
    }

    /// <summary>ENH-PDP-006 — Check if current user is subscribed to this product.</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetStatus(
        Guid productId,
        [FromQuery] Guid? variantId = null,
        CancellationToken ct = default)
    {
        var dto = await svc.GetSubscriptionAsync(productId, CurrentUserId, variantId, ct);
        if (dto is null)
            return Ok(new { subscribed = false });
        return Ok(new { subscribed = true, subscription = dto });
    }

    /// <summary>
    /// ENH-PDP-006 — Admin endpoint: trigger back-in-stock notifications for a product
    /// (called when stock is replenished).
    /// </summary>
    [HttpPost("notify")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> NotifySubscribers(
        Guid productId,
        CancellationToken ct = default)
    {
        var result = await svc.NotifySubscribersAsync(productId, ct);
        return Ok(result);
    }
}
