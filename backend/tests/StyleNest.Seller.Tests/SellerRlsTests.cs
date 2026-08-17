/**
 * ENH-SELL-001 — Multi-Tenant Row-Level Security (TSD §6.1 / AG-002)
 * Acceptance criteria:
 *   - HttpCurrentSellerContext resolves seller_id claim → Guid
 *   - HttpCurrentSellerContext: no claim → null
 *   - HttpCurrentSellerContext: invalid GUID string → null
 *   - HttpCurrentSellerContext: no HTTP context → null
 *   - SellerService.GetInventory scoped to sellerId (cross-tenant rows invisible)
 *   - SellerService.UpdateInventory wrong sellerId → throws KeyNotFoundException
 *   - SellerService.GetProducts scoped to sellerId
 *   - SellerService.UpdateProduct wrong sellerId → throws KeyNotFoundException
 *   - SellerService.DeleteProduct wrong sellerId → throws KeyNotFoundException
 */

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;
using StyleNest.Seller.API.Services;
using Xunit;
using SellerEntity = StyleNest.Infrastructure.Entities.Seller.Seller;

namespace StyleNest.Seller.Tests;

public sealed class SellerRlsTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SellerService _sut;
    private readonly Guid _sellerA = Guid.NewGuid();
    private readonly Guid _sellerB = Guid.NewGuid();

    public SellerRlsTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new SellerService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── HttpCurrentSellerContext ───────────────────────────────────────────────

    [Fact]
    public void HttpCurrentSellerContext_WithSellerIdClaim_ReturnsParsedGuid()
    {
        var expected = Guid.NewGuid();
        var ctx = BuildContextWithClaim("seller_id", expected.ToString());

        var sut = new HttpCurrentSellerContext(ctx);

        sut.SellerId.Should().Be(expected);
    }

    [Fact]
    public void HttpCurrentSellerContext_WithoutClaim_ReturnsNull()
    {
        var ctx = BuildContextWithClaim("some_other_claim", "value");

        var sut = new HttpCurrentSellerContext(ctx);

        sut.SellerId.Should().BeNull();
    }

    [Fact]
    public void HttpCurrentSellerContext_InvalidGuidString_ReturnsNull()
    {
        var ctx = BuildContextWithClaim("seller_id", "not-a-guid");

        var sut = new HttpCurrentSellerContext(ctx);

        sut.SellerId.Should().BeNull();
    }

    [Fact]
    public void HttpCurrentSellerContext_NullHttpContext_ReturnsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var sut = new HttpCurrentSellerContext(accessor.Object);

        sut.SellerId.Should().BeNull();
    }

    // ── Inventory isolation (application-layer RLS) ───────────────────────────

    [Fact]
    public async Task GetInventory_OnlyReturnsSellersOwnItems()
    {
        await SeedSellersAsync();
        var variantA = await SeedVariantAsync();
        var variantB = await SeedVariantAsync();

        _db.SellerInventories.Add(new SellerInventory
        {
            Id = Guid.NewGuid(), SellerId = _sellerA, ProductVariantId = variantA, Stock = 10, Price = 100m
        });
        _db.SellerInventories.Add(new SellerInventory
        {
            Id = Guid.NewGuid(), SellerId = _sellerB, ProductVariantId = variantB, Stock = 5, Price = 200m
        });
        await _db.SaveChangesAsync();

        var items = await _sut.GetInventoryAsync(_sellerA);

        items.Should().HaveCount(1);
        items.Single().Stock.Should().Be(10);
    }

    [Fact]
    public async Task UpdateInventory_WrongSellerId_ThrowsKeyNotFound()
    {
        await SeedSellersAsync();
        var variantA = await SeedVariantAsync();
        var inventoryId = Guid.NewGuid();
        _db.SellerInventories.Add(new SellerInventory
        {
            Id = inventoryId, SellerId = _sellerA, ProductVariantId = variantA, Stock = 10, Price = 100m
        });
        await _db.SaveChangesAsync();

        var act = async () => await _sut.UpdateInventoryAsync(
            _sellerB, inventoryId, new UpdateInventoryRequest(50, null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Product isolation ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_OnlyReturnsSellersOwnProducts()
    {
        var (catId, brandId) = await SeedCategoryAndBrandAsync();
        await SeedSellersAsync();
        SeedProduct(catId, brandId, _sellerA);
        SeedProduct(catId, brandId, _sellerA);
        SeedProduct(catId, brandId, _sellerB);
        await _db.SaveChangesAsync();

        var result = await _sut.GetProductsAsync(_sellerA, 1, 25);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(p => p.Should().NotBeNull());
    }

    [Fact]
    public async Task UpdateProduct_WrongSellerId_ThrowsKeyNotFound()
    {
        var (catId, brandId) = await SeedCategoryAndBrandAsync();
        await SeedSellersAsync();
        var productId = SeedProduct(catId, brandId, _sellerA);
        await _db.SaveChangesAsync();

        var act = async () => await _sut.UpdateProductAsync(
            _sellerB, productId,
            new UpdateSellerProductRequest("New Name", null, 999m, null, true, null));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not owned by seller*");
    }

    [Fact]
    public async Task DeleteProduct_WrongSellerId_ThrowsKeyNotFound()
    {
        var (catId, brandId) = await SeedCategoryAndBrandAsync();
        await SeedSellersAsync();
        var productId = SeedProduct(catId, brandId, _sellerA);
        await _db.SaveChangesAsync();

        var act = async () => await _sut.DeleteProductAsync(_sellerB, productId);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not owned by seller*");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IHttpContextAccessor BuildContextWithClaim(string claimType, string value)
    {
        var identity = new ClaimsIdentity([new Claim(claimType, value)], "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    private async Task SeedSellersAsync()
    {
        _db.Sellers.AddRange(
            new SellerEntity { Id = _sellerA, UserId = Guid.NewGuid(), StoreName = "StoreA", Slug = $"store-a-{_sellerA}", Status = SellerStatus.Active },
            new SellerEntity { Id = _sellerB, UserId = Guid.NewGuid(), StoreName = "StoreB", Slug = $"store-b-{_sellerB}", Status = SellerStatus.Active }
        );
        await _db.SaveChangesAsync();
    }

    private async Task<(Guid catId, Guid brandId)> SeedCategoryAndBrandAsync()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        _db.Categories.Add(new Category { Id = catId,   Name = "Cat",   Slug = $"cat-{catId}"   });
        _db.Brands.Add(new Brand        { Id = brandId, Name = "Brand", Slug = $"brand-{brandId}" });
        await _db.SaveChangesAsync();
        return (catId, brandId);
    }

    private async Task<Guid> SeedVariantAsync()
    {
        var variantId = Guid.NewGuid();
        _db.ProductVariants.Add(new ProductVariant
        {
            Id  = variantId, Sku = $"SKU-{variantId}", Size = "M", Colour = "Black", StockQuantity = 10
        });
        await _db.SaveChangesAsync();
        return variantId;
    }

    private Guid SeedProduct(Guid catId, Guid brandId, Guid sellerId)
    {
        var id = Guid.NewGuid();
        _db.Products.Add(new Product
        {
            Id         = id,
            Name       = $"Product-{id}",
            Slug       = $"product-{id}",
            BasePrice  = 500m,
            CategoryId = catId,
            BrandId    = brandId,
            SellerId   = sellerId,
        });
        return id;
    }
}
