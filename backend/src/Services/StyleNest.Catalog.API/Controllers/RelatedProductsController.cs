using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-PDP-005 / ENH-AI-004 — Related product rails on PDP.</summary>
[ApiController]
[Route("api/v1/products/{productId:guid}")]
public sealed class RelatedProductsController(IRelatedProductsService relatedService) : ControllerBase
{
    /// <summary>
    /// ENH-PDP-005/AI-004 — Returns all three related-product rails in one call:
    /// Similar (same category+brand), Complete the Look (same category, alt. brand),
    /// and Frequently Bought Together (order co-occurrence ranked).
    /// </summary>
    [HttpGet("related-rails")]
    public async Task<IActionResult> GetRelatedRails(Guid productId, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        var rails = await relatedService.GetRelatedRailsAsync(productId, limit, ct);
        return Ok(rails);
    }

    /// <summary>ENH-PDP-005 — Similar products (same category + brand).</summary>
    [HttpGet("similar")]
    public async Task<IActionResult> GetSimilar(Guid productId, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        var products = await relatedService.GetSimilarAsync(productId, limit, ct);
        return Ok(products);
    }

    /// <summary>ENH-PDP-005 — Complete the Look (same category, different brand).</summary>
    [HttpGet("complete-the-look")]
    public async Task<IActionResult> GetCompleteTheLook(Guid productId, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        var products = await relatedService.GetCompleteTheLookAsync(productId, limit, ct);
        return Ok(products);
    }

    /// <summary>ENH-AI-004 — Frequently Bought Together.</summary>
    [HttpGet("fbt")]
    public async Task<IActionResult> GetFbt(Guid productId, [FromQuery] int limit = 8, CancellationToken ct = default)
    {
        var products = await relatedService.GetFbtAsync(productId, limit, ct);
        return Ok(products);
    }
}
