using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Cart.API.Services;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;
using CartEntity = StyleNest.Infrastructure.Entities.Commerce.Cart;
using CartItemEntity = StyleNest.Infrastructure.Entities.Commerce.CartItem;

namespace StyleNest.Cart.Tests;

public sealed class CartServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CartService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(options);
        _sut = new CartService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid productId, Guid variantId)> SeedProductAsync(
        string sku = "SKU-001", decimal basePrice = 500m)
    {
        var brandId    = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId  = Guid.NewGuid();
        var variantId  = Guid.NewGuid();

        _db.Brands.Add(new Brand    { Id = brandId,    Name = "Test Brand",    Slug = $"brand-{brandId}"    });
        _db.Categories.Add(new Category { Id = categoryId, Name = "Test Category", Slug = $"cat-{categoryId}" });
        _db.Products.Add(new Product
        {
            Id         = productId,
            Name       = "Test Product",
            Slug       = $"product-{productId}",
            BasePrice  = basePrice,
            CategoryId = categoryId,
            BrandId    = brandId,
            IsActive   = true
        });
        _db.ProductVariants.Add(new ProductVariant
        {
            Id            = variantId,
            ProductId     = productId,
            Size          = "M",
            Colour        = "Red",
            Sku           = sku,
            StockQuantity = 20
        });
        await _db.SaveChangesAsync();
        return (productId, variantId);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCartAsync_NewUser_ReturnsEmptyCart()
    {
        var cart = await _sut.GetCartAsync(_userId);

        cart.Should().NotBeNull();
        cart.Items.Should().BeEmpty();
        cart.SubTotal.Should().Be(0m);
        cart.Total.Should().Be(0m);
    }

    [Fact]
    public async Task AddItemAsync_VariantNotFound_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.AddItemAsync(_userId, Guid.NewGuid(), "M", "Red", 1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Product variant not found*");
    }

    [Fact]
    public async Task AddItemAsync_ValidVariant_ReturnsCartWithOneItem()
    {
        var (productId, _) = await SeedProductAsync();

        var cart = await _sut.AddItemAsync(_userId, productId, "M", "Red", 2);

        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(2);
        cart.Items[0].UnitPrice.Should().Be(500m);
        cart.SubTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task AddItemAsync_SameVariantTwice_IncrementsQuantity()
    {
        var (productId, _) = await SeedProductAsync(sku: "SKU-INC");

        await _sut.AddItemAsync(_userId, productId, "M", "Red", 1);
        var cart = await _sut.AddItemAsync(_userId, productId, "M", "Red", 3);

        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(4);
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesFromCart()
    {
        var (productId, _) = await SeedProductAsync(sku: "SKU-REM");
        var cart = await _sut.AddItemAsync(_userId, productId, "M", "Red", 1);
        var itemId = cart.Items[0].Id;

        var emptyCart = await _sut.RemoveItemAsync(_userId, itemId);

        emptyCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyCouponAsync_InvalidCode_ThrowsInvalidOperationException()
    {
        var act = async () => await _sut.ApplyCouponAsync(_userId, "BADCODE");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*coupon*");
    }

    [Fact]
    public async Task ApplyCouponAsync_PercentageCoupon_AppliesDiscountToSubTotal()
    {
        var (productId, _) = await SeedProductAsync(sku: "SKU-COUP", basePrice: 1000m);

        // Put one item worth ₹1000 in the cart
        await _sut.AddItemAsync(_userId, productId, "M", "Red", 1);

        _db.Coupons.Add(new Coupon
        {
            Id            = Guid.NewGuid(),
            Code          = "SAVE10",
            Description   = "10% off",
            DiscountType  = DiscountType.Percentage,
            DiscountValue = 10m,
            IsActive      = true
        });
        await _db.SaveChangesAsync();

        var cart = await _sut.ApplyCouponAsync(_userId, "SAVE10");

        cart.CouponCode.Should().Be("SAVE10");
        cart.DiscountAmount.Should().Be(100m);
        cart.Total.Should().Be(900m);
    }
}
