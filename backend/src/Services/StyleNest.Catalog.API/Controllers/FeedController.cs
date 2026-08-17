using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-AI-001/002 — Personalised and trending product feed endpoints.</summary>
[ApiController]
[Route("api/v1/feed")]
public sealed class FeedController(IPersonalisedFeedService feedService) : ControllerBase
{
    /// <summary>
    /// ENH-AI-001 — Returns a personalised 12-product rail for the given user.
    /// Pass <c>userId</c> for logged-in users; omit for guests.
    /// The response includes <c>isPersonalised</c> and <c>reason</c> so the client
    /// can render the appropriate heading ("For You" vs "Trending Now").
    /// </summary>
    [HttpGet("personalised")]
    public async Task<IActionResult> GetPersonalised(
        [FromQuery] Guid? userId,
        [FromQuery] int   limit = 12,
        CancellationToken ct = default)
    {
        var feed = await feedService.GetFeedAsync(userId, limit, ct);
        return Ok(feed);
    }

    /// <summary>
    /// ENH-AI-002 — Returns trending products (most viewed in the last 7 days).
    /// Optionally scoped to a single category via <c>categoryId</c>.
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending(
        [FromQuery] Guid? categoryId,
        [FromQuery] int   limit = 12,
        CancellationToken ct = default)
    {
        var products = await feedService.GetTrendingAsync(categoryId, limit, ct);
        return Ok(products);
    }
}
