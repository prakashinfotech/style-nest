using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;

namespace StyleNest.Catalog.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class CategoriesController(
    ICatalogService catalogService,
    IValidator<CreateCategoryRequest> createValidator,
    ICategorySlugRedirectService slugRedirectService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await catalogService.GetCategoriesAsync(ct);
        return Ok(categories);
    }

    [HttpGet("{id:guid}/attributes")]
    [ProducesResponseType<IReadOnlyList<AttributeDefinitionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryAttributes(Guid id, CancellationToken ct)
    {
        var attrs = await catalogService.GetCategoryAttributesAsync(id, ct);
        return Ok(attrs);
    }

    [HttpPost("{id:guid}/attributes")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MapAttribute(Guid id, [FromBody] MapCategoryAttributeRequest req, CancellationToken ct)
    {
        await catalogService.MapCategoryAttributeAsync(id, req, ct);
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest req, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        var category = await catalogService.CreateCategoryAsync(req, ct);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    /// <summary>
    /// ENH-CAT-008 — Resolve a category by slug.
    /// Returns 200 when the slug is current, 301 when the slug is an old (renamed) slug,
    /// or 404 when the slug is completely unknown.
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var resolution = await slugRedirectService.ResolveAsync(slug, ct);

        if (resolution is null)
            return NotFound(new { message = $"Category slug '{slug}' not found." });

        if (resolution.NeedsRedirect)
        {
            var redirectUrl = Url.Action(nameof(GetBySlug),
                new { slug = resolution.CurrentSlug }) ?? $"/api/v1/categories/by-slug/{resolution.CurrentSlug}";
            return RedirectPermanent(redirectUrl);
        }

        return Ok(resolution.Category);
    }

    /// <summary>
    /// ENH-CAT-008 — Rename a category. The old slug is automatically preserved
    /// in CategorySlugHistory so existing URLs 301-redirect to the new slug.
    /// </summary>
    [HttpPut("{id:guid}/rename")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenameCategory(Guid id, [FromBody] RenameCategoryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewName))
            return BadRequest(new { message = "NewName is required." });

        var category = await slugRedirectService.RenameCategoryAsync(id, req.NewName, ct: ct);
        return category is null ? NotFound(new { message = $"Category {id} not found." }) : Ok(category);
    }
}

/// <summary>ENH-CAT-008 — Request body for the rename endpoint.</summary>
public sealed record RenameCategoryRequest(string NewName);
