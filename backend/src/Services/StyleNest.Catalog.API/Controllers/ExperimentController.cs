using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

/// <summary>ENH-CAT-003 — Response DTO for GET /api/v1/experiments/{name}/variant.</summary>
public sealed record ExperimentVariantResponse(string ExperimentName, string Variant);

/// <summary>
/// ENH-CAT-003 — A/B Variant Framework.
/// GET /api/v1/experiments/{name}/variant — returns the stable variant for a participant.
///
/// Query parameters:
///   participantId — stable anonymous or authenticated ID (UUID stored in localStorage)
///   variants      — comma-separated list of variant names, e.g. "A,B" or "control,treatment"
/// </summary>
[ApiController]
[Route("api/v1/experiments")]
[Produces("application/json")]
public sealed class ExperimentController(IExperimentService experiments) : ControllerBase
{
    /// <summary>
    /// Returns the deterministic variant assignment for the given participant.
    /// The same participantId + experimentName always returns the same variant.
    /// </summary>
    [HttpGet("{name}/variant")]
    [ProducesResponseType(typeof(ExperimentVariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetVariant(
        [FromRoute] string name,
        [FromQuery] string participantId,
        [FromQuery] string variants)
    {
        if (string.IsNullOrWhiteSpace(participantId))
            return BadRequest(new { message = "participantId is required." });

        if (string.IsNullOrWhiteSpace(variants))
            return BadRequest(new { message = "variants is required (comma-separated, e.g. 'A,B')." });

        var variantList = variants
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (variantList.Count < 2)
            return BadRequest(new { message = "At least 2 comma-separated variants are required." });

        var variant = experiments.AssignVariant(name, participantId, variantList);
        return Ok(new ExperimentVariantResponse(name, variant));
    }
}
