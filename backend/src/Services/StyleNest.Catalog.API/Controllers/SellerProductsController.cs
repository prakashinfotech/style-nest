using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/seller/products")]
[Authorize(Roles = "Seller")]
public sealed class SellerProductsController(
    ISellerCatalogService sellerCatalogService,
    IValidator<CreateSellerProductRequest> createValidator,
    IValidator<UpdateSellerProductRequest> updateValidator) : ControllerBase
{
    private Guid SellerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SellerProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProducts(CancellationToken ct)
    {
        var products = await sellerCatalogService.GetMyProductsAsync(SellerId, ct);
        return Ok(products);
    }

    [HttpPost]
    [ProducesResponseType<SellerProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateSellerProductRequest req, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await sellerCatalogService.CreateProductAsync(SellerId, req, ct);
        return StatusCode(StatusCodes.Status201Created, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SellerProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateSellerProductRequest req, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await sellerCatalogService.UpdateProductAsync(SellerId, id, req, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        var deleted = await sellerCatalogService.DeleteProductAsync(SellerId, id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
