using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-CAT-007 — SEO canonicalisation endpoints.</summary>
[ApiController]
[Route("api/v1/seo")]
public sealed class SeoController(ISeoService seoService) : ControllerBase
{
    /// <summary>
    /// ENH-CAT-007 — Returns SEO metadata (title, meta-description, canonical path)
    /// for a product page. Uses stored overrides where available; falls back to
    /// auto-generated templates from product/brand data.
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetProductSeo(Guid productId, CancellationToken ct)
    {
        var seo = await seoService.GetProductSeoAsync(productId, ct);
        if (seo is null)
            return NotFound(new { message = $"Product {productId} not found." });

        return Ok(seo);
    }

    /// <summary>
    /// ENH-CAT-007 — Returns SEO metadata for a category page.
    /// Uses stored overrides where available; falls back to auto-generated templates.
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetCategorySeo(Guid categoryId, CancellationToken ct)
    {
        var seo = await seoService.GetCategorySeoAsync(categoryId, ct);
        if (seo is null)
            return NotFound(new { message = $"Category {categoryId} not found." });

        return Ok(seo);
    }
}
