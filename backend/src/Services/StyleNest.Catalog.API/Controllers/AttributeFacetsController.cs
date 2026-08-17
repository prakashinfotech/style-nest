/**
 * ENH-ADMIN-005 — Dynamic Attribute Filtering: facets endpoint.
 *
 * GET api/v1/products/facets?categoryId={id}&brandId={id}&search={q}
 *   Returns all filterable attribute facets (name + distinct values + count)
 *   for the given category scope.  Drives the left-hand filter sidebar on PLP.
 */

using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class AttributeFacetsController(IDynamicAttributeFilterService facetSvc) : ControllerBase
{
    /// <summary>
    /// ENH-ADMIN-005 — Returns attribute facets for the given category.
    /// Facets contain distinct values + product counts to power the filter sidebar.
    /// </summary>
    [HttpGet("facets")]
    public async Task<IActionResult> GetFacets(
        [FromQuery] Guid    categoryId,
        [FromQuery] Guid?   brandId  = null,
        [FromQuery] string? search   = null,
        CancellationToken   ct       = default)
    {
        var result = await facetSvc.GetFacetsAsync(categoryId, brandId, search, ct);
        return Ok(result);
    }
}
