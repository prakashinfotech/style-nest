/**
 * ENH-PDP-005 — Related Products Rails (Similar, Complete the Look, FBT)
 * ENH-AI-004  — AI-Powered Related Products (Frequently Bought Together)
 * Acceptance criteria tested here:
 *   Similar:
 *     - Returns products with same CategoryId + BrandId, excluding source, ordered by rating
 *     - Excludes inactive products
 *     - Returns empty list for unknown productId
 *   Complete the Look:
 *     - Returns products with same CategoryId but different BrandId, ordered by review count
 *     - Excludes the source product
 *   FBT (ENH-AI-004):
 *     - Returns products co-purchased with source, ranked by order co-occurrence count
 *     - Returns empty list when no co-purchase history exists
 *     - Higher-scoring co-purchased product ranks first
 *     - Excludes the source product from FBT results
 *   GetRelatedRailsAsync:
 *     - Returns all three rails in one call
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class RelatedProductsServiceTests : IDisposable
{
    private readonly AppDbContext _db;

    public RelatedProductsServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private RelatedProductsService BuildSut() =>
        new(_db, NullLogger<RelatedProductsService>.Instance);

    private Product MakeProduct(Guid catId, Guid brandId, string name = "P",
        bool isActive = true, double rating = 4.0, int reviews = 10)
    {
        var p = new Product
        {
            Id            = Guid.NewGuid(),
            Name          = name,
            Slug          = $"{name.ToLower()}-{Guid.NewGuid():N}",
            BasePrice     = 999m,
            CategoryId    = catId,
            BrandId       = brandId,
            IsActive      = isActive,
            AverageRating = rating,
            ReviewCount   = reviews,
        };
        _db.Products.Add(p);
        return p;
    }

    private ProductVariant MakeVariant(Guid productId)
    {
        var v = new ProductVariant
        {
            Id            = Guid.NewGuid(),
            ProductId     = productId,
            Size          = "M",
            Sku           = $"SKU-{Guid.NewGuid():N}",
            StockQuantity = 10,
        };
        _db.ProductVariants.Add(v);
        return v;
    }

    private async Task<Infrastructure.Entities.Orders.Order> MakeOrderWithItemsAsync(
        params Guid[] variantIds)
    {
        var order = new Infrastructure.Entities.Orders.Order
        {
            Id          = Guid.NewGuid(),
            OrderNumber = $"TC-{Guid.NewGuid():N}",
            UserId      = Guid.NewGuid(),
            Status      = OrderStatus.Delivered,
        };
        _db.Orders.Add(order);

        foreach (var vid in variantIds)
        {
            _db.OrderItems.Add(new OrderItem
            {
                Id               = Guid.NewGuid(),
                OrderId          = order.Id,
                ProductVariantId = vid,
                ProductName      = "Test",
                Quantity         = 1,
                UnitPrice        = 999m,
                TotalPrice       = 999m,
            });
        }

        await _db.SaveChangesAsync();
        return order;
    }

    // ── GetSimilarAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSimilar_ReturnsSameCategoryAndBrand()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source");
        var similar = MakeProduct(catId, brandId, "Similar");
        MakeProduct(Guid.NewGuid(), brandId, "DiffCat");  // different category
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetSimilarAsync(source.Id);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(similar.Id);
    }

    [Fact]
    public async Task GetSimilar_ExcludesInactiveProducts()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source   = MakeProduct(catId, brandId, "Source");
        MakeProduct(catId, brandId, "Inactive", isActive: false);
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetSimilarAsync(source.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSimilar_OrderedByRatingDescending()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source",  rating: 3.0);
        var lowRat  = MakeProduct(catId, brandId, "LowRat",  rating: 3.5, reviews: 10);
        var highRat = MakeProduct(catId, brandId, "HighRat", rating: 4.8, reviews: 5);
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetSimilarAsync(source.Id);

        result[0].ProductId.Should().Be(highRat.Id, "highest-rated first");
        result[1].ProductId.Should().Be(lowRat.Id);
    }

    [Fact]
    public async Task GetSimilar_UnknownProduct_ReturnsEmpty()
    {
        var result = await BuildSut().GetSimilarAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ── GetCompleteTheLookAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetCompleteTheLook_ReturnsSameCategoryDifferentBrand()
    {
        var catId    = Guid.NewGuid();
        var brandA   = Guid.NewGuid();
        var brandB   = Guid.NewGuid();
        var source   = MakeProduct(catId, brandA, "Source");
        var altBrand = MakeProduct(catId, brandB, "AltBrand");
        MakeProduct(catId, brandA, "SameBrand");  // same brand → excluded
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetCompleteTheLookAsync(source.Id);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(altBrand.Id);
    }

    [Fact]
    public async Task GetCompleteTheLook_OrderedByReviewCountDesc()
    {
        var catId   = Guid.NewGuid();
        var brandA  = Guid.NewGuid();
        var brandB  = Guid.NewGuid();
        var brandC  = Guid.NewGuid();
        var source  = MakeProduct(catId, brandA, "Source");
        var popular = MakeProduct(catId, brandB, "Popular", reviews: 500);
        var niche   = MakeProduct(catId, brandC, "Niche",   reviews: 10);
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetCompleteTheLookAsync(source.Id);

        result[0].ProductId.Should().Be(popular.Id, "most-reviewed alt brand first");
    }

    // ── GetFbtAsync (ENH-AI-004) ──────────────────────────────────────────────

    [Fact]
    public async Task GetFbt_ReturnsCourchasedProducts()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source");
        var coP     = MakeProduct(catId, brandId, "Co-Purchased");

        var sourceV = MakeVariant(source.Id);
        var coPV    = MakeVariant(coP.Id);
        await MakeOrderWithItemsAsync(sourceV.Id, coPV.Id);

        var result = await BuildSut().GetFbtAsync(source.Id);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(coP.Id);
    }

    [Fact]
    public async Task GetFbt_NoPurchaseHistory_ReturnsEmpty()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source");
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetFbtAsync(source.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFbt_RankedByCoOccurrenceCount()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source");
        var commonP = MakeProduct(catId, brandId, "Common");   // bought with source in 3 orders
        var rareP   = MakeProduct(catId, brandId, "Rare");     // bought with source in 1 order

        var sourceV = MakeVariant(source.Id);
        var commonV = MakeVariant(commonP.Id);
        var rareV   = MakeVariant(rareP.Id);

        await MakeOrderWithItemsAsync(sourceV.Id, commonV.Id);
        await MakeOrderWithItemsAsync(sourceV.Id, commonV.Id);
        await MakeOrderWithItemsAsync(sourceV.Id, commonV.Id);
        await MakeOrderWithItemsAsync(sourceV.Id, rareV.Id);

        var result = await BuildSut().GetFbtAsync(source.Id);

        result[0].ProductId.Should().Be(commonP.Id,
            "most co-purchased product should rank first");
        result[1].ProductId.Should().Be(rareP.Id);
    }

    [Fact]
    public async Task GetFbt_SourceProductNotInResults()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var source  = MakeProduct(catId, brandId, "Source");
        var other   = MakeProduct(catId, brandId, "Other");

        var sourceV = MakeVariant(source.Id);
        var otherV  = MakeVariant(other.Id);
        await MakeOrderWithItemsAsync(sourceV.Id, otherV.Id);

        var result = await BuildSut().GetFbtAsync(source.Id);

        result.Should().NotContain(p => p.ProductId == source.Id);
    }

    [Fact]
    public async Task GetFbt_UnknownProduct_ReturnsEmpty()
    {
        var result = await BuildSut().GetFbtAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ── GetRelatedRailsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRelatedRails_ReturnsAllThreeRails()
    {
        var catId   = Guid.NewGuid();
        var brandA  = Guid.NewGuid();
        var brandB  = Guid.NewGuid();
        var source  = MakeProduct(catId, brandA, "Source");
        MakeProduct(catId, brandA, "Similar");
        MakeProduct(catId, brandB, "CompleteTheLook");
        await _db.SaveChangesAsync();

        var rails = await BuildSut().GetRelatedRailsAsync(source.Id);

        rails.Similar.Should().HaveCount(1);
        rails.CompleteTheLook.Should().HaveCount(1);
        rails.FrequentlyBoughtTogether.Should().BeEmpty();  // no order history seeded
    }
}
