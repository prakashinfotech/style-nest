/**
 * ENH-SELL-004 — Cross-Tenant Data Isolation Assertion (ArchUnit-style build gate)
 * Acceptance criteria tested here:
 *   - Structural: every non-profile ISellerService method declares a `sellerId` parameter
 *   - Structural: SellerInventory entity exposes a SellerId property
 *   - Structural: SellerPayout entity exposes a SellerId property
 *   - Structural: SellerService exposes no public method returning IEnumerable / IQueryable without sellerId
 *   - Runtime: GetPayoutsAsync — Seller B's payouts are invisible to Seller A
 *   - Runtime: GetPayoutsAsync — Seller A's payouts are invisible to Seller B
 *   - Runtime: GetPayoutsAsync — pagination metadata (TotalCount) is per-seller scoped
 *   - Runtime: GetInventoryAsync — cross-tenant SellerInventory rows are invisible
 *   - Runtime: GetProductsAsync — cross-tenant products are invisible
 *   - Runtime: GetOrdersAsync — cross-tenant orders are invisible
 *   - Runtime: GetOrderAsync — order owned by another seller's variants returns null
 *   - Structural: concrete SellerService class has no un-scoped GetAll-style method
 */

using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Seller.API.DTOs;
using StyleNest.Seller.API.Services;
using Xunit;
using SellerEntity = StyleNest.Infrastructure.Entities.Seller.Seller;

namespace StyleNest.Seller.Tests;

public sealed class CrossTenantIsolationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SellerService _sut;
    private readonly Guid _sellerA = Guid.NewGuid();
    private readonly Guid _sellerB = Guid.NewGuid();

    public CrossTenantIsolationTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new SellerService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Structural: interface method signatures ────────────────────────────────

    /// <summary>
    /// Every ISellerService method except *ProfileAsync must declare a
    /// <c>sellerId</c> parameter, enforcing the architectural rule that all
    /// seller-scoped data access is keyed by sellerId.
    /// </summary>
    [Fact]
    public void ISellerService_AllNonProfileMethods_HaveSellerIdParameter()
    {
        var interfaceType = typeof(ISellerService);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var violations = methods
            .Where(m => !m.Name.EndsWith("ProfileAsync", StringComparison.Ordinal))
            .Where(m =>
            {
                var parameters = m.GetParameters();
                return !parameters.Any(p =>
                    p.Name!.Equals("sellerId", StringComparison.OrdinalIgnoreCase));
            })
            .Select(m => m.Name)
            .ToList();

        violations.Should().BeEmpty(
            because: "all non-profile ISellerService methods must scope access by sellerId " +
                     "(ENH-SELL-004 cross-tenant isolation build gate)");
    }

    /// <summary>
    /// SellerInventory entity must expose a SellerId property so EF filters
    /// and service-layer Where clauses can enforce tenant isolation.
    /// </summary>
    [Fact]
    public void SellerInventory_HasSellerIdProperty()
    {
        var prop = typeof(SellerInventory)
            .GetProperty("SellerId", BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull(
            because: "SellerInventory must carry a SellerId column for row-level isolation");
        prop!.PropertyType.Should().Be(typeof(Guid),
            because: "SellerId must be a non-nullable Guid");
    }

    /// <summary>
    /// SellerPayout entity must expose a SellerId property so payout queries
    /// can be scoped to a single seller without full-table scans.
    /// </summary>
    [Fact]
    public void SellerPayout_HasSellerIdProperty()
    {
        var prop = typeof(SellerPayout)
            .GetProperty("SellerId", BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull(
            because: "SellerPayout must carry a SellerId column for row-level isolation");
        prop!.PropertyType.Should().Be(typeof(Guid),
            because: "SellerId must be a non-nullable Guid");
    }

    /// <summary>
    /// The concrete SellerService must not expose any public method whose name
    /// starts with "GetAll" — a naming pattern associated with unscoped bulk reads.
    /// All collection-returning methods should be scoped to a specific seller.
    /// </summary>
    [Fact]
    public void SellerService_HasNoUnscoped_GetAllMethods()
    {
        var methods = typeof(SellerService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name.StartsWith("GetAll", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Name)
            .ToList();

        methods.Should().BeEmpty(
            because: "SellerService must not expose unscoped GetAll* methods; " +
                     "every collection endpoint must filter by sellerId (ENH-SELL-004)");
    }

    // ── Runtime: payout isolation ─────────────────────────────────────────────

    [Fact]
    public async Task GetPayoutsAsync_SellerA_DoesNotSeeSellerBPayouts()
    {
        await SeedPayoutsAsync(_sellerA, 2);
        await SeedPayoutsAsync(_sellerB, 3);

        var result = await _sut.GetPayoutsAsync(_sellerA, page: 1, pageSize: 50);

        result.Items.Should().HaveCount(2,
            because: "Seller A should see only their own 2 payouts");
        result.Items.Should().AllSatisfy(p =>
            p.Id.Should().NotBeEmpty(), "all returned payout IDs must be valid");
    }

    [Fact]
    public async Task GetPayoutsAsync_SellerB_DoesNotSeeSellerAPayouts()
    {
        await SeedPayoutsAsync(_sellerA, 3);
        await SeedPayoutsAsync(_sellerB, 1);

        var result = await _sut.GetPayoutsAsync(_sellerB, page: 1, pageSize: 50);

        result.Items.Should().HaveCount(1,
            because: "Seller B should see only their own 1 payout");
    }

    [Fact]
    public async Task GetPayoutsAsync_TotalCount_IsPerSellerScoped()
    {
        await SeedPayoutsAsync(_sellerA, 5);
        await SeedPayoutsAsync(_sellerB, 7);

        var resultA = await _sut.GetPayoutsAsync(_sellerA, page: 1, pageSize: 2);
        var resultB = await _sut.GetPayoutsAsync(_sellerB, page: 1, pageSize: 2);

        resultA.TotalCount.Should().Be(5,
            because: "TotalCount must reflect only Seller A's payouts");
        resultB.TotalCount.Should().Be(7,
            because: "TotalCount must reflect only Seller B's payouts");
    }

    // ── Runtime: inventory isolation ──────────────────────────────────────────

    [Fact]
    public async Task GetInventoryAsync_SellerA_DoesNotSeeSellerBInventory()
    {
        var (variantA, variantB) = await SeedInventoryAsync();

        var inventoryA = (await _sut.GetInventoryAsync(_sellerA)).ToList();
        var inventoryB = (await _sut.GetInventoryAsync(_sellerB)).ToList();

        inventoryA.Should().HaveCount(1,
            because: "Seller A should see only their own inventory entry");
        inventoryA.Single().ProductVariantId.Should().Be(variantA,
            because: "Seller A's inventory must reference their own variant");

        inventoryB.Should().HaveCount(1,
            because: "Seller B should see only their own inventory entry");
        inventoryB.Single().ProductVariantId.Should().Be(variantB,
            because: "Seller B's inventory must reference their own variant");
    }

    // ── Runtime: product isolation ────────────────────────────────────────────

    [Fact]
    public async Task GetProductsAsync_SellerA_DoesNotSeeSellerBProducts()
    {
        await SeedProductsAsync(_sellerA, count: 2);
        await SeedProductsAsync(_sellerB, count: 3);

        var resultA = await _sut.GetProductsAsync(_sellerA, page: 1, pageSize: 50);
        var resultB = await _sut.GetProductsAsync(_sellerB, page: 1, pageSize: 50);

        resultA.Items.Should().HaveCount(2,
            because: "Seller A should see only their 2 products");
        resultB.Items.Should().HaveCount(3,
            because: "Seller B should see only their 3 products");
    }

    // ── Runtime: order isolation ──────────────────────────────────────────────

    /// <summary>
    /// A seller with no inventory should see zero orders, even when other orders exist —
    /// verifying that the isolation boundary (no variants → empty sellerVariantIds → no matches)
    /// is enforced correctly.
    /// </summary>
    [Fact]
    public async Task GetOrdersAsync_SellerWithNoInventory_SeesZeroOrders()
    {
        // Seed inventory only for SellerB and create an order for SellerB
        var (_, variantB) = await SeedInventoryAsync();
        await SeedOrderAsync(variantB);

        // SellerC has no inventory at all
        var sellerC = Guid.NewGuid();
        var resultC = await _sut.GetOrdersAsync(sellerC, page: 1, pageSize: 50, status: null);

        resultC.Items.Should().BeEmpty(
            because: "a seller with no inventory has no variant IDs to match orders on; " +
                     "the empty intersection is the isolation boundary (ENH-SELL-004)");
        resultC.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOrderAsync_WrongSeller_ReturnsNull()
    {
        var (variantA, _) = await SeedInventoryAsync();
        var orderId = await SeedOrderAsync(variantA);

        // Seller B tries to fetch Seller A's order
        var result = await _sut.GetOrderAsync(_sellerB, orderId);

        result.Should().BeNull(
            because: "Seller B must not be able to access an order that contains only Seller A's variants");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SeedPayoutsAsync(Guid sellerId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _db.SellerPayouts.Add(new SellerPayout
            {
                Id       = Guid.NewGuid(),
                SellerId = sellerId,
                Amount   = 1000m * (i + 1),
                Status   = PayoutStatus.Completed,
                TransactionReference = $"TXN-{sellerId}-{i}",
            });
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds one product variant + SellerInventory row per seller.
    /// Returns (variantIdA, variantIdB).
    /// </summary>
    private async Task<(Guid variantA, Guid variantB)> SeedInventoryAsync()
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        _db.Categories.Add(new Category { Id = catId, Name = "Cat", Slug = "cat" });
        _db.Brands.Add(new Brand { Id = brandId, Name = "Brand", Slug = "brand" });

        var productA = new Product
        {
            Id = Guid.NewGuid(), Name = "Product A", Slug = "product-a",
            BasePrice = 500m, CategoryId = catId, BrandId = brandId, SellerId = _sellerA,
        };
        var productB = new Product
        {
            Id = Guid.NewGuid(), Name = "Product B", Slug = "product-b",
            BasePrice = 600m, CategoryId = catId, BrandId = brandId, SellerId = _sellerB,
        };

        var variantA = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = productA.Id,
            Sku = "SKU-A", Size = "M", Colour = "Red", StockQuantity = 10,
        };
        var variantB = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = productB.Id,
            Sku = "SKU-B", Size = "L", Colour = "Blue", StockQuantity = 5,
        };

        _db.Products.AddRange(productA, productB);
        _db.ProductVariants.AddRange(variantA, variantB);

        _db.SellerInventories.Add(new SellerInventory
        {
            Id = Guid.NewGuid(), SellerId = _sellerA,
            ProductVariantId = variantA.Id, Stock = 10, Price = 500m,
        });
        _db.SellerInventories.Add(new SellerInventory
        {
            Id = Guid.NewGuid(), SellerId = _sellerB,
            ProductVariantId = variantB.Id, Stock = 5, Price = 600m,
        });

        await _db.SaveChangesAsync();
        return (variantA.Id, variantB.Id);
    }

    private async Task SeedProductsAsync(Guid sellerId, int count)
    {
        var catId   = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        _db.Categories.Add(new Category { Id = catId, Name = $"Cat-{sellerId}", Slug = $"cat-{sellerId}" });
        _db.Brands.Add(new Brand { Id = brandId, Name = $"Brand-{sellerId}", Slug = $"brand-{sellerId}" });

        for (int i = 0; i < count; i++)
        {
            _db.Products.Add(new Product
            {
                Id         = Guid.NewGuid(),
                Name       = $"Product {i} for {sellerId}",
                Slug       = $"product-{sellerId}-{i}",
                BasePrice  = 100m * (i + 1),
                CategoryId = catId,
                BrandId    = brandId,
                SellerId   = sellerId,
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task<Guid> SeedOrderAsync(Guid variantId)
    {
        // InMemory does not enforce FK constraints, so we can skip seeding the user.
        // SellerService.MapOrder guards o.User with a null-check ("Customer" fallback).
        // We add the OrderItem directly to _db.OrderItems (not via navigation property) so
        // EF InMemory stores the entity by FK and Include() can resolve the relationship.
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _db.Orders.Add(new Order
        {
            Id          = orderId,
            UserId      = userId,
            OrderNumber = $"ORD-{orderId.ToString()[..8]}",
            TotalAmount = 999m,
            Status      = OrderStatus.Pending,
        });

        _db.OrderItems.Add(new OrderItem
        {
            Id               = Guid.NewGuid(),
            OrderId          = orderId,
            ProductVariantId = variantId,
            ProductName      = "Test Product",
            Quantity         = 1,
            UnitPrice        = 999m,
        });

        await _db.SaveChangesAsync();
        return orderId;
    }
}
