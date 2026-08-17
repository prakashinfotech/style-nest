using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;

namespace StyleNest.Admin.API.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = "Admin")]
public sealed class AdminProductsController(
    IAdminService adminService,
    IValidator<UpdateProductStatusRequest> statusValidator,
    IProductDescriptionAssistant descriptionAssistant) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var products = await adminService.GetAdminProductsAsync(ct);
        return Ok(products);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProductStatus(Guid id, [FromBody] UpdateProductStatusRequest req, CancellationToken ct)
    {
        var validation = await statusValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await adminService.UpdateProductStatusAsync(id, req.IsActive, ct);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// ENH-AI-003 — Generate a product description using Azure OpenAI GPT-4.
    /// POST /api/v1/admin/products/generate-description
    /// </summary>
    [HttpPost("generate-description")]
    [ProducesResponseType(typeof(GenerateDescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDescription(
        [FromBody] GenerateDescriptionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
            return BadRequest(new { code = "MISSING_PRODUCT_NAME", message = "ProductName is required." });

        var result = await descriptionAssistant.GenerateAsync(request, ct);
        return Ok(result);
    }
}
