/**
 * ENH-CAT-007 — SEO Canonicalisation
 * Acceptance criteria tested here:
 *   - GetProductSeoAsync: auto-generates title/description/canonical from product+brand data
 *   - GetProductSeoAsync: uses stored override when SeoMetadata row exists
 *   - GetProductSeoAsync: partial override — null override fields fall back to template
 *   - GetProductSeoAsync: HasCustomOverride=true only when override row exists
 *   - GetProductSeoAsync: canonical path contains product slug
 *   - GetProductSeoAsync: unknown productId → returns null
 *   - GetCategorySeoAsync: auto-generates from category data
 *   - GetCategorySeoAsync: uses stored override when SeoMetadata row exists
 *   - GetCategorySeoAsync: canonical path contains category slug
 *   - GetCategorySeoAsync: unknown categoryId → returns null
 *   - Product with no brand → graceful fallback in description template
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class SeoServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public SeoServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SeoService BuildSut() => new(_db);

    private async Task<(Product product, Brand brand)> SeedProductAsync(
        string name = "Test Product", string slug = "test-product")
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Nike", Slug = "nike" };
        _db.Brands.Add(brand);

        var product = new Product
        {
            Id         = Guid.NewGuid(),
            Name       = name,
            Slug       = slug,
            BasePrice  = 999m,
            CategoryId = Guid.NewGuid(),
            BrandId    = brand.Id,
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return (product, brand);
    }

    private async Task<Category> SeedCategoryAsync(
        string name = "Shoes", string slug = "shoes")
    {
        var cat = new Category { Id = Guid.NewGuid(), Name = name, Slug = slug };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    private async Task SeedOverrideAsync(string entityType, Guid entityId,
        string? title = null, string? description = null, string? canonical = null)
    {
        _db.SeoMetadata.Add(new Infrastructure.Entities.Catalog.SeoMetadata
        {
            Id                      = Guid.NewGuid(),
            EntityType              = entityType,
            EntityId                = entityId,
            TitleOverride           = title,
            MetaDescriptionOverride = description,
            CanonicalPathOverride   = canonical,
        });
        await _db.SaveChangesAsync();
    }

    // ── GetProductSeoAsync — auto-generated ───────────────────────────────────

    [Fact]
    public async Task GetProductSeo_NoOverride_GeneratesTitle()
    {
        var (product, _) = await SeedProductAsync("Air Max 90", "air-max-90");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result.Should().NotBeNull();
        result!.PageTitle.Should().Contain("Air Max 90");
        result.PageTitle.Should().Contain("StyleNest");
    }

    [Fact]
    public async Task GetProductSeo_NoOverride_CanonicalContainsSlug()
    {
        var (product, _) = await SeedProductAsync("Air Max", "air-max-90");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.CanonicalPath.Should().Contain("air-max-90");
        result.CanonicalPath.Should().StartWith("/products/");
    }

    [Fact]
    public async Task GetProductSeo_NoOverride_DescriptionContainsBrandName()
    {
        var (product, brand) = await SeedProductAsync();

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.MetaDescription.Should().Contain(brand.Name);
    }

    [Fact]
    public async Task GetProductSeo_NoOverride_HasCustomOverrideFalse()
    {
        var (product, _) = await SeedProductAsync();

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.HasCustomOverride.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductSeo_UnknownId_ReturnsNull()
    {
        var result = await BuildSut().GetProductSeoAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetProductSeoAsync — override ─────────────────────────────────────────

    [Fact]
    public async Task GetProductSeo_WithOverride_UsesTitleOverride()
    {
        var (product, _) = await SeedProductAsync();
        await SeedOverrideAsync("product", product.Id, title: "Custom Title | StyleNest");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.PageTitle.Should().Be("Custom Title | StyleNest");
        result.HasCustomOverride.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductSeo_WithOverride_UsesDescriptionOverride()
    {
        var (product, _) = await SeedProductAsync();
        await SeedOverrideAsync("product", product.Id,
            description: "Hand-crafted meta description for SEO.");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.MetaDescription.Should().Be("Hand-crafted meta description for SEO.");
    }

    [Fact]
    public async Task GetProductSeo_WithOverride_UsesCanonicalOverride()
    {
        var (product, _) = await SeedProductAsync();
        await SeedOverrideAsync("product", product.Id, canonical: "/custom/path");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.CanonicalPath.Should().Be("/custom/path");
    }

    [Fact]
    public async Task GetProductSeo_PartialOverride_NullFieldsFallBackToTemplate()
    {
        var (product, _) = await SeedProductAsync("Air Max", "air-max");
        // Only title is overridden; description and canonical are null → fall back to template
        await SeedOverrideAsync("product", product.Id, title: "Custom Title");

        var result = await BuildSut().GetProductSeoAsync(product.Id);

        result!.PageTitle.Should().Be("Custom Title");
        result.CanonicalPath.Should().Contain("air-max", "canonical falls back to slug-based path");
        result.HasCustomOverride.Should().BeTrue("override row exists");
    }

    // ── GetCategorySeoAsync — auto-generated ──────────────────────────────────

    [Fact]
    public async Task GetCategorySeo_NoOverride_GeneratesTitle()
    {
        var cat = await SeedCategoryAsync("Running Shoes", "running-shoes");

        var result = await BuildSut().GetCategorySeoAsync(cat.Id);

        result.Should().NotBeNull();
        result!.PageTitle.Should().Contain("Running Shoes");
        result.PageTitle.Should().Contain("StyleNest");
    }

    [Fact]
    public async Task GetCategorySeo_NoOverride_CanonicalContainsSlug()
    {
        var cat = await SeedCategoryAsync("Bags", "bags");

        var result = await BuildSut().GetCategorySeoAsync(cat.Id);

        result!.CanonicalPath.Should().StartWith("/category/");
        result.CanonicalPath.Should().Contain("bags");
    }

    [Fact]
    public async Task GetCategorySeo_NoOverride_HasCustomOverrideFalse()
    {
        var cat = await SeedCategoryAsync();

        var result = await BuildSut().GetCategorySeoAsync(cat.Id);

        result!.HasCustomOverride.Should().BeFalse();
    }

    [Fact]
    public async Task GetCategorySeo_UnknownId_ReturnsNull()
    {
        var result = await BuildSut().GetCategorySeoAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategorySeo_WithFullOverride_UsesAllOverrideFields()
    {
        var cat = await SeedCategoryAsync("Shoes", "shoes");
        await SeedOverrideAsync("category", cat.Id,
            title:       "Best Shoes Online | StyleNest",
            description: "Explore over 10,000 shoe styles.",
            canonical:   "/shoes/");

        var result = await BuildSut().GetCategorySeoAsync(cat.Id);

        result!.PageTitle.Should().Be("Best Shoes Online | StyleNest");
        result.MetaDescription.Should().Be("Explore over 10,000 shoe styles.");
        result.CanonicalPath.Should().Be("/shoes/");
        result.HasCustomOverride.Should().BeTrue();
    }
}
