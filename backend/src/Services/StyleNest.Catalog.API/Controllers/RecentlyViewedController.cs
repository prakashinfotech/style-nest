/**
 * ENH-CAT-001 — Recently Viewed Products Rail endpoints.
 * Two routes:
 *   POST api/v1/products/{productId}/view  — record a view (called when user lands on PDP)
 *   GET  api/v1/feed/recently-viewed       — fetch the user's last-12 rail
 */

using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-CAT-001 — Recently viewed product rail (record + retrieve).</summary>
[ApiController]
public sealed class RecentlyViewedController(
    IRecentlyViewedService recentlyViewedService) : ControllerBase
{
    /// <summary>
    /// ENH-CAT-001 — Record a product view.
    /// Accepts either a <c>userId</c> (authenticated) or a <c>sessionId</c> (anonymous).
    /// Upserts the view and enforces the 12-entry cap automatically.
    /// </summary>
    [HttpPost("api/v1/products/{productId:guid}/view")]
    public async Task<IActionResult> RecordView(
        Guid productId,
        [FromQuery] Guid?   userId    = null,
        [FromQuery] string? sessionId = null,
        CancellationToken ct = default)
    {
        if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { message = "Provide userId or sessionId." });

        await recentlyViewedService.RecordViewAsync(userId, sessionId, productId, ct: ct);
        return NoContent();
    }

    /// <summary>
    /// ENH-CAT-001 — Retrieve the recently-viewed rail.
    /// Returns up to 12 active products ordered newest-first.
    /// </summary>
    [HttpGet("api/v1/feed/recently-viewed")]
    public async Task<IActionResult> GetRecentlyViewed(
        [FromQuery] Guid?   userId    = null,
        [FromQuery] string? sessionId = null,
        [FromQuery] int     limit     = 12,
        CancellationToken ct = default)
    {
        if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { message = "Provide userId or sessionId." });

        var items = await recentlyViewedService.GetRecentlyViewedAsync(userId, sessionId, limit, ct);
        return Ok(items);
    }
}
