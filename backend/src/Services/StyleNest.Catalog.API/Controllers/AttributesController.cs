using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/attributes")]
public sealed class AttributesController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AttributeDefinitionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttributes(CancellationToken ct)
    {
        var attrs = await catalogService.GetAllAttributesAsync(ct);
        return Ok(attrs);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType<AttributeDefinitionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAttribute([FromBody] CreateAttributeDefinitionRequest req, CancellationToken ct)
    {
        var attr = await catalogService.CreateAttributeDefinitionAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, attr);
    }
}
