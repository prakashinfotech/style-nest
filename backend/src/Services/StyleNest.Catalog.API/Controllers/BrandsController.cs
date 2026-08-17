using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class BrandsController(
    ICatalogService catalogService,
    IValidator<CreateBrandRequest> createValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BrandDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands(CancellationToken ct)
    {
        var brands = await catalogService.GetBrandsAsync(ct);
        return Ok(brands);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<BrandDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest req, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var brand = await catalogService.CreateBrandAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, brand);
    }
}
