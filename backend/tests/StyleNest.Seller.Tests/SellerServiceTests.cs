using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;
using StyleNest.Seller.API.Services;
using Xunit;
using SellerEntity = StyleNest.Infrastructure.Entities.Seller.Seller;

namespace StyleNest.Seller.Tests;

public sealed class SellerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SellerService _sut;
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _sellerUserId = Guid.NewGuid();

    public SellerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(options);
        _sut = new SellerService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid categoryId, Guid brandId)> SeedCategoryAndBrandAsync()
    {
        var categoryId = Guid.NewGuid();
        var brandId    = Guid.NewGuid();

        _db.Categories.Add(new Category { Id = categoryId, Name = "Test Category", Slug = $"cat-{categoryId}" });
        _db.Brands.Add(new Brand        { Id = brandId,    Name = "Test Brand",    Slug = $"brand-{brandId}"   });
        await _db.SaveChangesAsync();
        return (categoryId, brandId);
    }

    private async Task SeedSellerAsync()
    {
        _db.Sellers.Add(new SellerEntity
        {
            Id        = _sellerId,
            UserId    = _sellerUserId,
            StoreName = "Test Store",
            Slug      = $"store-{_sellerId}",
            Status    = SellerStatus.Active
        });
        await _db.SaveChangesAsync();
    }

    private async Task<Guid> SeedProductForSellerAsync(Guid? overrideSellerId = null)
    {
        var (categoryId, brandId) = await SeedCategoryAndBrandAsync();
        var productId = Guid.NewGuid();

        _db.Products.Add(new Product
        {
            Id         = productId,
            Name       = "Seller Product",
            Slug       = $"seller-product-{productId}",
            BasePrice  = 800m,
            CategoryId = categoryId,
            BrandId    = brandId,
            SellerId   = overrideSellerId ?? _sellerId,
            IsActive   = false
        });
        await _db.SaveChangesAsync();
        return productId;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProductAsync_ValidRequest_CreatesProductWithVariantsAndInventory()
    {
        var (categoryId, brandId) = await SeedCategoryAndBrandAsync();

        var request = new CreateSellerProductRequest(
            Name:            "New Dress",
            Description:     "Beautiful dress",
            BasePrice:       1200m,
            DiscountedPrice: null,
            CategoryId:      categoryId,
            BrandId:         brandId,
            Variants: [new CreateVariantRequest("SKU-NEW-001", "S", "Blue", 10, null)],
            ImageUrls: ["https://example.com/image1.jpg"],
            Attributes: null
        );

        var result = await _sut.CreateProductAsync(_sellerId, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Dress");
        result.BasePrice.Should().Be(1200m);
        result.TotalStock.Should().Be(10);
        result.IsActive.Should().BeFalse();

        // Verify inventory entry was created
        var inventory = await _db.SellerInventories.FirstOrDefaultAsync(si => si.SellerId == _sellerId);
        inventory.Should().NotBeNull();
        inventory!.Stock.Should().Be(10);
    }

    [Fact]
    public async Task CreateProductAsync_MultipleVariants_CreatesInventoryForEach()
    {
        var (categoryId, brandId) = await SeedCategoryAndBrandAsync();

        var request = new CreateSellerProductRequest(
            Name:            "Multi-variant Shirt",
            Description:     null,
            BasePrice:       500m,
            DiscountedPrice: null,
            CategoryId:      categoryId,
            BrandId:         brandId,
            Variants:
            [
                new CreateVariantRequest("SKU-MV-001", "S", "White", 5, null),
                new CreateVariantRequest("SKU-MV-002", "M", "White", 8, null),
                new CreateVariantRequest("SKU-MV-003", "L", "White", 3, null)
            ],
            ImageUrls: [],
            Attributes: null
        );

        var result = await _sut.CreateProductAsync(_sellerId, request);

        result.TotalStock.Should().Be(16);
        _db.SellerInventories.Count(si => si.SellerId == _sellerId).Should().Be(3);
    }

    [Fact]
    public async Task UpdateProductAsync_NonOwnerProduct_ThrowsKeyNotFoundException()
    {
        var otherSellerId = Guid.NewGuid();
        var productId = await SeedProductForSellerAsync(); // belongs to _sellerId

        var request = new UpdateSellerProductRequest(
            "Updated Name", null, 900m, null, true, null);

        var act = async () => await _sut.UpdateProductAsync(otherSellerId, productId, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not owned by seller*");
    }

    [Fact]
    public async Task UpdateProductAsync_OwnProduct_UpdatesSuccessfully()
    {
        var productId = await SeedProductForSellerAsync();

        var request = new UpdateSellerProductRequest(
            "Updated Name", "New description", 950m, 850m, true, null);

        var result = await _sut.UpdateProductAsync(_sellerId, productId, request);

        result.Name.Should().Be("Updated Name");
        result.BasePrice.Should().Be(950m);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_OwnProduct_SoftDeletesProduct()
    {
        var productId = await SeedProductForSellerAsync();

        await _sut.DeleteProductAsync(_sellerId, productId);

        var product = await _db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == productId);
        product.Should().NotBeNull();
        product!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_NonOwnerProduct_ThrowsKeyNotFoundException()
    {
        var otherSellerId = Guid.NewGuid();
        var productId = await SeedProductForSellerAsync(); // belongs to _sellerId

        var act = async () => await _sut.DeleteProductAsync(otherSellerId, productId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateInventoryAsync_ValidRequest_UpdatesStockAndPrice()
    {
        await SeedSellerAsync();
        var (_, _) = await SeedCategoryAndBrandAsync();

        var variantId     = Guid.NewGuid();
        var inventoryId   = Guid.NewGuid();

        _db.ProductVariants.Add(new ProductVariant
        {
            Id            = variantId,
            ProductId     = Guid.NewGuid(),
            Size          = "M",
            Sku           = "SKU-INV-001",
            StockQuantity = 5
        });
        _db.SellerInventories.Add(new SellerInventory
        {
            Id               = inventoryId,
            SellerId         = _sellerId,
            ProductVariantId = variantId,
            Stock            = 5,
            Price            = 500m
        });
        await _db.SaveChangesAsync();

        var request = new UpdateInventoryRequest(Stock: 25, Price: 550m, LowStockThreshold: 10);
        var result = await _sut.UpdateInventoryAsync(_sellerId, inventoryId, request);

        result.Stock.Should().Be(25);
        result.Price.Should().Be(550m);
        result.LowStockThreshold.Should().Be(10);
        result.IsLowStock.Should().BeFalse();

        var variant = await _db.ProductVariants.FindAsync(variantId);
        variant!.StockQuantity.Should().Be(25);
    }

    [Fact]
    public async Task UpdateInventoryAsync_NonOwnerInventory_ThrowsKeyNotFoundException()
    {
        var act = async () =>
            await _sut.UpdateInventoryAsync(_sellerId, Guid.NewGuid(), new UpdateInventoryRequest(10, null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Inventory item not found*");
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsOnlySellerProducts()
    {
        var otherSellerId = Guid.NewGuid();
        await SeedProductForSellerAsync();
        await SeedProductForSellerAsync(overrideSellerId: otherSellerId);

        var result = await _sut.GetProductsAsync(_sellerId, 1, 10);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }
}
