/**
 * ENH-PDP-003 — Size Guide Modal endpoints.
 *
 * GET    api/v1/brands/{brandId}/size-guide?categoryId={id}  → fetch guide (anon)
 * PUT    api/v1/brands/{brandId}/size-guide                  → upsert [Admin]
 * DELETE api/v1/brands/{brandId}/size-guide?categoryId={id}  → soft-delete [Admin]
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/brands/{brandId:guid}/size-guide")]
public sealed class SizeGuideController(ISizeGuideService svc) : ControllerBase
{
    /// <summary>ENH-PDP-003 — Fetch size guide for brand + optional category (fallback to brand-wide).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        Guid brandId,
        [FromQuery] Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var dto = await svc.GetAsync(brandId, categoryId, ct);
        if (dto is null)
            return NotFound(new { message = $"No size guide found for brand {brandId}." });

        return Ok(dto);
    }

    /// <summary>ENH-PDP-003 — Create or update size guide (Admin only).</summary>
    [HttpPut]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Upsert(
        Guid brandId,
        [FromBody] UpsertSizeGuideRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var dto = await svc.UpsertAsync(brandId, request, ct);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>ENH-PDP-003 — Soft-delete size guide (Admin only).</summary>
    [HttpDelete]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(
        Guid brandId,
        [FromQuery] Guid? categoryId = null,
        CancellationToken ct = default)
    {
        await svc.DeleteAsync(brandId, categoryId, ct);
        return NoContent();
    }
}
