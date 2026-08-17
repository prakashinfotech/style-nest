/**
 * ENH-SRCH-003 — Search Synonyms Dictionary: Admin CMS endpoints.
 *
 * GET    api/v1/admin/search/synonyms              → list all active synonyms
 * PUT    api/v1/admin/search/synonyms              → create or update a synonym entry
 * DELETE api/v1/admin/search/synonyms/{id}         → soft-delete
 * GET    api/v1/admin/search/synonyms/expand?term= → expand a term into its synonyms
 *
 * All endpoints require Admin or SuperAdmin role.
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/search/synonyms")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class SearchSynonymController(ISearchSynonymService svc) : ControllerBase
{
    /// <summary>ENH-SRCH-003 — List all active synonym entries ordered by term.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await svc.GetAllAsync(ct));

    /// <summary>ENH-SRCH-003 — Create or update a synonym entry (upsert by term).</summary>
    [HttpPut]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertSynonymRequest req,
        CancellationToken ct = default)
    {
        try
        {
            var dto = await svc.UpsertAsync(req, ct);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>ENH-SRCH-003 — Soft-delete a synonym entry by id.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await svc.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>ENH-SRCH-003 — Expand a term into its synonym list.</summary>
    [HttpGet("expand")]
    public async Task<IActionResult> Expand(
        [FromQuery] string term,
        CancellationToken ct = default)
    {
        var synonyms = await svc.ExpandAsync(term, ct);
        return Ok(new { term, synonyms });
    }
}
