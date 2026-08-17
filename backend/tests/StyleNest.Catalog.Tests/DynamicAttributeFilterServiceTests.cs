/**
 * ENH-ADMIN-005 — Dynamic Attribute Filtering: DynamicAttributeFilterService + EAV query in CatalogService
 *
 * Acceptance criteria (Phase 9.7):
 *   TC-ADMIN-005-01: GetFacets — returns facets for filterable attributes in category
 *   TC-ADMIN-005-02: GetFacets — value counts reflect only active products in category
 *   TC-ADMIN-005-03: GetFacets — multi-value facet lists distinct values + counts
 *   TC-ADMIN-005-04: GetFacets — non-filterable attributes excluded from facets
 *   TC-ADMIN-005-05: GetFacets — returns empty facets when category has no attributes
 *   TC-ADMIN-005-06: GetFacets — brand filter scopes facet counts to that brand
 *   TC-ADMIN-005-07: GetFacets — search term scopes facet counts to matching products
 *   TC-ADMIN-005-08: GetProductsAsync — single attribute filter returns matching products only
 *   TC-ADMIN-005-09: GetProductsAsync — multi-attribute AND filter returns intersection
 *   TC-ADMIN-005-10: GetProductsAsync — OR within single attribute returns union
 *   TC-ADMIN-005-11: GetProductsAsync — attribute filter combined with category filter
 *   TC-ADMIN-005-12: GetProductsAsync — no matches returns empty paged result
 *   TC-ADMIN-005-13: GetFacets — inactive products not counted in facets
 *   TC-ADMIN-005-14: GetFacets — returns empty list when no products in category
 */

using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StyleNest.Catalog.API.DTOs;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class DynamicAttributeFilterServiceTests : IDisposable
{
    private readonly AppDbContext                  _db;
    private readonly DynamicAttributeFilterService _facetSvc;

    // Fixed IDs
    private static readonly Guid CatId1  = Guid.Parse("11110000-0000-0000-0000-000000000001");
    private static readonly Guid CatId2  = Guid.Parse("11110000-0000-0000-0000-000000000002");
    private static readonly Guid BrandId1 = Guid.Parse("22220000-0000-0000-0000-000000000001");
    private static readonly Guid BrandId2 = Guid.Parse("22220000-0000-0000-0000-000000000002");

    // Attribute IDs
    private static readonly Guid ColorAttrId    = Guid.Parse("33330000-0000-0000-0000-000000000001");
    private static readonly Guid MaterialAttrId = Guid.Parse("33330000-0000-0000-0000-000000000002");
    private static readonly Guid HiddenAttrId   = Guid.Parse("33330000-0000-0000-0000-000000000003");

    // Product IDs
    private static readonly Guid Prod1 = Guid.Parse("44440000-0000-0000-0000-000000000001");
    private static readonly Guid Prod2 = Guid.Parse("44440000-0000-0000-0000-000000000002");
    private static readonly Guid Prod3 = Guid.Parse("44440000-0000-0000-0000-000000000003");
    private static readonly Guid Prod4 = Guid.Parse("44440000-0000-0000-0000-000000000004"); // brand2
    private static readonly Guid Prod5 = Guid.Parse("44440000-0000-0000-0000-000000000005"); // inactive

    public DynamicAttributeFilterServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db       = new AppDbContext(opts);
        _facetSvc = new DynamicAttributeFilterService(_db);
        Seed();
    }

    public void Dispose() => _db.Dispose();

    // ─── GetFacets tests ──────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-ADMIN-005-01: GetFacets — returns facets for filterable attributes")]
    public async Task GetFacets_ReturnsFacets()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1);

        result.CategoryId.Should().Be(CatId1);
        result.Facets.Should().HaveCount(2); // Color + Material (Hidden excluded)
        result.Facets.Select(f => f.Name).Should().BeEquivalentTo(["Color", "Material"]);
    }

    [Fact(DisplayName = "TC-ADMIN-005-02: GetFacets — value counts reflect only active products")]
    public async Task GetFacets_CountsActiveProductsOnly()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1);

        var colorFacet = result.Facets.First(f => f.Name == "Color");
        // Prod1=Red, Prod2=Blue, Prod3=Red, Prod4=Red (brand2, cat1), Prod5(inactive, Red)
        // Active in Cat1: Prod1,Prod2,Prod3,Prod4
        var redCount = colorFacet.Values.FirstOrDefault(v => v.Value == "Red")?.Count ?? 0;
        redCount.Should().Be(3); // Prod1, Prod3, Prod4
    }

    [Fact(DisplayName = "TC-ADMIN-005-03: GetFacets — multi-value facet lists distinct values + counts")]
    public async Task GetFacets_DistinctValuesWithCounts()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1);

        var colorFacet = result.Facets.First(f => f.Name == "Color");
        colorFacet.Values.Should().HaveCount(2); // Red, Blue
        colorFacet.Values.Should().Contain(v => v.Value == "Blue" && v.Count == 1);
        colorFacet.Values.Should().Contain(v => v.Value == "Red"  && v.Count == 3);
    }

    [Fact(DisplayName = "TC-ADMIN-005-04: GetFacets — non-filterable attributes excluded")]
    public async Task GetFacets_ExcludesNonFilterable()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1);

        result.Facets.Should().NotContain(f => f.Name == "HiddenAttr");
    }

    [Fact(DisplayName = "TC-ADMIN-005-05: GetFacets — empty facets when category has no attributes")]
    public async Task GetFacets_EmptyWhenNoAttributes()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId2);

        result.Facets.Should().BeEmpty();
    }

    [Fact(DisplayName = "TC-ADMIN-005-06: GetFacets — brand filter scopes facet counts")]
    public async Task GetFacets_BrandFilterScopes()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1, brandId: BrandId1);

        var colorFacet = result.Facets.First(f => f.Name == "Color");
        // Brand1 products in Cat1: Prod1(Red), Prod2(Blue), Prod3(Red)
        colorFacet.Values.Should().Contain(v => v.Value == "Red"  && v.Count == 2);
        colorFacet.Values.Should().Contain(v => v.Value == "Blue" && v.Count == 1);
    }

    [Fact(DisplayName = "TC-ADMIN-005-07: GetFacets — search term scopes facet counts")]
    public async Task GetFacets_SearchScopes()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1, search: "Silk");

        // Only Prod3 has "Silk" in name
        var colorFacet = result.Facets.FirstOrDefault(f => f.Name == "Color");
        colorFacet.Should().NotBeNull();
        colorFacet!.Values.Should().HaveCount(1);
        colorFacet.Values[0].Value.Should().Be("Red");
        colorFacet.Values[0].Count.Should().Be(1);
    }

    [Fact(DisplayName = "TC-ADMIN-005-13: GetFacets — inactive products not counted")]
    public async Task GetFacets_InactiveProductsExcluded()
    {
        var result = await _facetSvc.GetFacetsAsync(CatId1);
        var colorFacet = result.Facets.First(f => f.Name == "Color");

        // Prod5 is inactive with Red — count for Red should not include it
        // Active Red products: Prod1, Prod3, Prod4 = 3
        colorFacet.Values.First(v => v.Value == "Red").Count.Should().Be(3);
    }

    [Fact(DisplayName = "TC-ADMIN-005-14: GetFacets — returns empty when no products in category")]
    public async Task GetFacets_EmptyWhenNoProducts()
    {
        var emptyCatId = Guid.NewGuid();
        var result = await _facetSvc.GetFacetsAsync(emptyCatId);

        result.Facets.Should().BeEmpty();
    }

    // ─── EAV filter via CatalogService.GetProductsAsync ──────────────────────

    [Fact(DisplayName = "TC-ADMIN-005-08: GetProductsAsync — single attribute filter returns matching products")]
    public async Task GetProducts_SingleAttributeFilter()
    {
        var svc = new CatalogService(_db, CreateMapperMock(), new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());

        var query = new ProductQueryDto
        {
            CategoryId       = CatId1,
            AttributeFilters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Color"] = ["Blue"],
            },
        };

        var result = await svc.GetProductsAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items[0].Id.Should().Be(Prod2);
    }

    [Fact(DisplayName = "TC-ADMIN-005-09: GetProductsAsync — multi-attribute AND filter returns intersection")]
    public async Task GetProducts_MultiAttributeAndFilter()
    {
        var svc = new CatalogService(_db, CreateMapperMock(), new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());

        var query = new ProductQueryDto
        {
            CategoryId       = CatId1,
            AttributeFilters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Color"]    = ["Red"],
                ["Material"] = ["Cotton"],
            },
        };

        var result = await svc.GetProductsAsync(query);

        // Prod1: Red + Cotton → matches; Prod3: Red + Silk → doesn't match Material=Cotton
        result.TotalCount.Should().Be(1);
        result.Items[0].Id.Should().Be(Prod1);
    }

    [Fact(DisplayName = "TC-ADMIN-005-10: GetProductsAsync — OR within single attribute returns union")]
    public async Task GetProducts_OrWithinAttribute()
    {
        var svc = new CatalogService(_db, CreateMapperMock(), new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());

        var query = new ProductQueryDto
        {
            CategoryId       = CatId1,
            AttributeFilters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Color"] = ["Red", "Blue"],
            },
        };

        var result = await svc.GetProductsAsync(query);

        // Red: Prod1, Prod3, Prod4  Blue: Prod2 → 4 total (Prod5 inactive)
        result.TotalCount.Should().Be(4);
    }

    [Fact(DisplayName = "TC-ADMIN-005-11: GetProductsAsync — attribute filter combined with category filter")]
    public async Task GetProducts_AttributeAndCategoryFilter()
    {
        var svc = new CatalogService(_db, CreateMapperMock(), new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());

        var query = new ProductQueryDto
        {
            CategoryId       = CatId1,
            BrandId          = BrandId2,
            AttributeFilters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Color"] = ["Red"],
            },
        };

        var result = await svc.GetProductsAsync(query);

        // Brand2 + Cat1 + Red = Prod4 only
        result.TotalCount.Should().Be(1);
        result.Items[0].Id.Should().Be(Prod4);
    }

    [Fact(DisplayName = "TC-ADMIN-005-12: GetProductsAsync — no matches returns empty paged result")]
    public async Task GetProducts_NoMatchReturnsEmpty()
    {
        var svc = new CatalogService(_db, CreateMapperMock(), new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());

        var query = new ProductQueryDto
        {
            CategoryId       = CatId1,
            AttributeFilters = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Color"] = ["Purple"], // no product has Purple
            },
        };

        var result = await svc.GetProductsAsync(query);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ─── Seed ─────────────────────────────────────────────────────────────────

    private void Seed()
    {
        // Categories
        _db.Categories.AddRange(
            new Category { Id = CatId1, Name = "Fashion", Slug = "fashion" },
            new Category { Id = CatId2, Name = "Footwear", Slug = "footwear" });

        // Brands
        _db.Brands.AddRange(
            new Brand { Id = BrandId1, Name = "BrandA", Slug = "brand-a" },
            new Brand { Id = BrandId2, Name = "BrandB", Slug = "brand-b" });

        // Attribute definitions
        _db.AttributeDefinitions.AddRange(
            new AttributeDefinition { Id = ColorAttrId,    Name = "Color",      DisplayName = "Colour",   DataType = "Select", IsFilterable = true },
            new AttributeDefinition { Id = MaterialAttrId, Name = "Material",   DisplayName = "Material", DataType = "Select", IsFilterable = true },
            new AttributeDefinition { Id = HiddenAttrId,   Name = "HiddenAttr", DisplayName = "Hidden",   DataType = "Text",   IsFilterable = false });

        // Map Color + Material to CatId1 (NOT to CatId2)
        _db.CategoryAttributes.AddRange(
            new CategoryAttribute { Id = Guid.NewGuid(), CategoryId = CatId1, AttributeDefinitionId = ColorAttrId },
            new CategoryAttribute { Id = Guid.NewGuid(), CategoryId = CatId1, AttributeDefinitionId = MaterialAttrId },
            new CategoryAttribute { Id = Guid.NewGuid(), CategoryId = CatId1, AttributeDefinitionId = HiddenAttrId });

        // Products: Prod1-4 active, Prod5 inactive — all in CatId1
        _db.Products.AddRange(
            MakeProd(Prod1, "Cotton Kurti",     CatId1, BrandId1),
            MakeProd(Prod2, "Blue Jeans",       CatId1, BrandId1),
            MakeProd(Prod3, "Silk Saree",       CatId1, BrandId1),
            MakeProd(Prod4, "Red Blazer",       CatId1, BrandId2),
            MakeProd(Prod5, "Old Red Shirt",    CatId1, BrandId1, active: false));

        _db.SaveChanges();

        // Product attributes
        void AddAttr(Guid productId, Guid attrId, string value) =>
            _db.ProductAttributes.Add(new ProductAttribute
            {
                Id = Guid.NewGuid(), ProductId = productId,
                AttributeDefinitionId = attrId, Value = value,
            });

        AddAttr(Prod1, ColorAttrId,    "Red");
        AddAttr(Prod1, MaterialAttrId, "Cotton");
        AddAttr(Prod2, ColorAttrId,    "Blue");
        AddAttr(Prod2, MaterialAttrId, "Denim");
        AddAttr(Prod3, ColorAttrId,    "Red");
        AddAttr(Prod3, MaterialAttrId, "Silk");
        AddAttr(Prod4, ColorAttrId,    "Red");
        AddAttr(Prod4, MaterialAttrId, "Polyester");
        AddAttr(Prod5, ColorAttrId,    "Red");  // inactive product — should not count
        AddAttr(Prod5, MaterialAttrId, "Cotton");

        _db.SaveChanges();
    }

    private static Product MakeProd(Guid id, string name, Guid catId, Guid brandId, bool active = true) =>
        new()
        {
            Id         = id,
            Name       = name,
            Slug       = name.ToLowerInvariant().Replace(" ", "-"),
            BasePrice  = 999m,
            CategoryId = catId,
            BrandId    = brandId,
            IsActive   = active,
        };

    private static IMapper CreateMapperMock()
    {
        var mock = new Mock<IMapper>();
        mock.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns<Product>(p => new ProductDto(
                p.Id, p.Name, p.Slug,
                p.Description ?? string.Empty,
                p.BasePrice, p.DiscountedPrice,
                p.BrandId, string.Empty,
                p.CategoryId, string.Empty,
                new List<string>(),
                new List<ProductVariantDto>(),
                new List<ProductAttributeDto>(),
                p.AverageRating, p.ReviewCount, p.IsActive));
        return mock.Object;
    }
}
