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

public sealed class CatalogServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CatalogService _sut;

    public CatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
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

        _sut = new CatalogService(_db, _mapperMock.Object, new NullCacheService(),
            Mock.Of<ISearchAnalyticsService>());
    }

    public void Dispose() => _db.Dispose();

    private async Task<(Brand brand, Category category)> SeedAsync()
    {
        var brand    = new Brand    { Id = Guid.NewGuid(), Name = "Test Brand",    Slug = "test-brand"    };
        var category = new Category { Id = Guid.NewGuid(), Name = "Test Category", Slug = "test-category" };
        _db.Brands.Add(brand);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return (brand, category);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsPagedResult()
    {
        var (brand, category) = await SeedAsync();

        _db.Products.AddRange(
            new Product { Id = Guid.NewGuid(), Name = "Product A", Slug = "product-a", BasePrice = 100, BrandId = brand.Id, CategoryId = category.Id, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Product B", Slug = "product-b", BasePrice = 200, BrandId = brand.Id, CategoryId = category.Id, IsActive = true }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetProductsAsync(new ProductQueryDto { Page = 1, PageSize = 24 });

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(24);
    }

    [Fact]
    public async Task GetProductAsync_ValidId_ReturnsProduct()
    {
        var (brand, category) = await SeedAsync();
        var productId = Guid.NewGuid();

        _db.Products.Add(new Product
        {
            Id = productId, Name = "My Product", Slug = "my-product",
            BasePrice = 500, BrandId = brand.Id, CategoryId = category.Id, IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetProductAsync(productId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Name.Should().Be("My Product");
        result.Price.Should().Be(500);
    }

    [Fact]
    public async Task GetProductAsync_InvalidId_ReturnsNull()
    {
        var result = await _sut.GetProductAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── PDP-10: GetProductAsync_ReturnsNull_WhenNotFound ──────────────────

    [Fact]
    public async Task GetProductAsync_ReturnsNull_WhenNotFound()
    {
        // No products seeded — any GUID should return null
        var result = await _sut.GetProductAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── PDP-10: GetRelatedProductsAsync_ReturnsSameCategory ───────────────

    [Fact]
    public async Task GetRelatedProductsAsync_ReturnsSameCategory_ExcludesCurrentProduct()
    {
        var (brand, category) = await SeedAsync();

        var targetId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();

        // Different category
        var otherCategory = new Category { Id = otherCategoryId, Name = "Other", Slug = "other" };
        _db.Categories.Add(otherCategory);

        _db.Products.AddRange(
            new Product { Id = targetId,  Name = "Target",  Slug = "target",  BasePrice = 100, BrandId = brand.Id, CategoryId = category.Id,      IsActive = true, AverageRating = 4.0 },
            new Product { Id = relatedId, Name = "Related", Slug = "related", BasePrice = 200, BrandId = brand.Id, CategoryId = category.Id,      IsActive = true, AverageRating = 4.5 },
            new Product { Id = Guid.NewGuid(), Name = "Other Cat", Slug = "other-cat", BasePrice = 300, BrandId = brand.Id, CategoryId = otherCategoryId, IsActive = true }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetRelatedProductsAsync(targetId, limit: 6);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(relatedId);
        result.Should().NotContain(p => p.Id == targetId);
    }

    [Fact]
    public async Task GetRelatedProductsAsync_RespectsLimit()
    {
        var (brand, category) = await SeedAsync();
        var targetId = Guid.NewGuid();

        _db.Products.Add(new Product { Id = targetId, Name = "Target", Slug = "target", BasePrice = 100, BrandId = brand.Id, CategoryId = category.Id, IsActive = true });

        for (var i = 0; i < 10; i++)
        {
            _db.Products.Add(new Product
            {
                Id = Guid.NewGuid(), Name = $"Related {i}", Slug = $"related-{i}",
                BasePrice = 100 + i, BrandId = brand.Id, CategoryId = category.Id, IsActive = true
            });
        }
        await _db.SaveChangesAsync();

        var result = await _sut.GetRelatedProductsAsync(targetId, limit: 4);

        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetRelatedProductsAsync_ReturnsEmpty_WhenProductNotFound()
    {
        var result = await _sut.GetRelatedProductsAsync(Guid.NewGuid(), limit: 6);

        result.Should().BeEmpty();
    }

    // ── PDP-10: CreateReviewAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateReviewAsync_PersistsReviewAndUpdatesProductRating()
    {
        var (brand, category) = await SeedAsync();
        var productId = Guid.NewGuid();
        var userId    = Guid.NewGuid();

        _db.Products.Add(new Product
        {
            Id = productId, Name = "Reviewed Product", Slug = "reviewed",
            BasePrice = 500, BrandId = brand.Id, CategoryId = category.Id, IsActive = true,
            AverageRating = 0, ReviewCount = 0
        });
        await _db.SaveChangesAsync();

        _mapperMock
            .Setup(m => m.Map<ReviewDto>(It.IsAny<StyleNest.Infrastructure.Entities.Catalog.Review>()))
            .Returns<StyleNest.Infrastructure.Entities.Catalog.Review>(r => new ReviewDto(
                r.Id, r.ProductId, r.UserId, r.Author, r.Rating, r.Title, r.Body, r.CreatedAt));

        var req = new CreateReviewRequest(5, "Excellent", "Really loved this product");
        var result = await _sut.CreateReviewAsync(productId, userId, "Alice", req);

        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        result.Title.Should().Be("Excellent");
        result.Author.Should().Be("Alice");
        result.ProductId.Should().Be(productId);

        // Product aggregate rating should be updated
        var product = await _db.Products.FindAsync(productId);
        product!.ReviewCount.Should().Be(1);
        product.AverageRating.Should().BeApproximately(5.0, 0.01);
    }

    [Fact]
    public async Task GetReviewsAsync_ReturnsPaginatedReviews()
    {
        var (brand, category) = await SeedAsync();
        var productId = Guid.NewGuid();

        _db.Products.Add(new Product
        {
            Id = productId, Name = "P", Slug = "p",
            BasePrice = 100, BrandId = brand.Id, CategoryId = category.Id, IsActive = true
        });

        for (var i = 0; i < 5; i++)
        {
            _db.Reviews.Add(new StyleNest.Infrastructure.Entities.Catalog.Review
            {
                Id = Guid.NewGuid(), ProductId = productId, UserId = Guid.NewGuid(),
                Author = $"User{i}", Rating = 4, Title = $"Review {i}", Body = "Body"
            });
        }
        await _db.SaveChangesAsync();

        _mapperMock
            .Setup(m => m.Map<ReviewDto>(It.IsAny<StyleNest.Infrastructure.Entities.Catalog.Review>()))
            .Returns<StyleNest.Infrastructure.Entities.Catalog.Review>(r => new ReviewDto(
                r.Id, r.ProductId, r.UserId, r.Author, r.Rating, r.Title, r.Body, r.CreatedAt));

        var result = await _sut.GetReviewsAsync(productId, page: 1, pageSize: 3);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
    }
}
