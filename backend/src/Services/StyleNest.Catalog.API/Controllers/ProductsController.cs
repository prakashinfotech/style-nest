using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;
using StyleNest.SharedKernel.DTOs;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ProductsController(
    ICatalogService catalogService,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    IValidator<CreateReviewRequest> reviewValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto query, CancellationToken ct)
    {
        var result = await catalogService.GetProductsAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var product = await catalogService.GetProductAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("{id:guid}/variants")]
    [ProducesResponseType<IReadOnlyList<ProductVariantDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariants(Guid id, CancellationToken ct)
    {
        var product = await catalogService.GetProductAsync(id, ct);
        if (product is null) return NotFound();

        var variants = await catalogService.GetVariantsAsync(id, ct);
        return Ok(variants);
    }

    [HttpGet("{id:guid}/related")]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelated(
        Guid id,
        [FromQuery] int limit = 6,
        CancellationToken ct = default)
    {
        var product = await catalogService.GetProductAsync(id, ct);
        if (product is null) return NotFound();

        var related = await catalogService.GetRelatedProductsAsync(id, limit, ct);
        return Ok(related);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest req, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await catalogService.CreateProductAsync(req, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest req, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await catalogService.UpdateProductAsync(id, req, ct);
        return product is null ? NotFound() : Ok(product);
    }

    // ── Reviews ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/reviews")]
    [ProducesResponseType<PagedResult<ReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var product = await catalogService.GetProductAsync(id, ct);
        if (product is null) return NotFound();

        var reviews = await catalogService.GetReviewsAsync(id, page, pageSize, ct);
        return Ok(reviews);
    }

    [HttpPost("{id:guid}/reviews")]
    [Authorize]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostReview(Guid id, [FromBody] CreateReviewRequest req, CancellationToken ct)
    {
        var validation = await reviewValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var product = await catalogService.GetProductAsync(id, ct);
        if (product is null) return NotFound();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var author = User.FindFirstValue(ClaimTypes.Name)
                  ?? User.FindFirstValue(ClaimTypes.Email)
                  ?? "Anonymous";

        var review = await catalogService.CreateReviewAsync(id, userId, author, req, ct);
        return CreatedAtAction(nameof(GetReviews), new { id }, review);
    }
}
