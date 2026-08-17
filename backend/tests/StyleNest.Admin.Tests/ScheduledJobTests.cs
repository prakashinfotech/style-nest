/**
 * ENH-ADMIN-003 — Scheduled Jobs: DailyAnalyticsJob, LowStockAlertJob,
 *                                  CartAbandonmentJob, ExpireCouponsJob
 * Phase 9.8 deferred
 *
 * Acceptance criteria tested here:
 *
 *   TC-ADMIN-JOB-001: DailyAnalytics → seeds order from yesterday → creates DailyRevenue row
 *   TC-ADMIN-JOB-002: DailyAnalytics → excluded cancelled orders → revenue = 0
 *   TC-ADMIN-JOB-003: DailyAnalytics → excludes today's orders (not yesterday)
 *   TC-ADMIN-JOB-004: DailyAnalytics → idempotent: re-run updates existing row, no duplicate
 *   TC-ADMIN-JOB-005: DailyAnalytics → no orders yesterday → row with Revenue=0, OrderCount=0
 *   TC-ADMIN-JOB-006: LowStock → items below threshold → ItemsAffected = count
 *   TC-ADMIN-JOB-007: LowStock → items above threshold → not counted
 *   TC-ADMIN-JOB-008: LowStock → items with Stock=0 → not counted (out-of-stock, separate concern)
 *   TC-ADMIN-JOB-009: CartAbandonment → idle cart with items → counted
 *   TC-ADMIN-JOB-010: CartAbandonment → recently active cart → not counted
 *   TC-ADMIN-JOB-011: CartAbandonment → empty cart (no items) → not counted
 *   TC-ADMIN-JOB-012: ExpireCoupons → expired active coupon → deactivated (IsActive=false)
 *   TC-ADMIN-JOB-013: ExpireCoupons → non-expired active coupon → untouched
 *   TC-ADMIN-JOB-014: ExpireCoupons → returns correct count of deactivated coupons
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Admin.API.Services;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Commerce;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Entities.Seller;
using StyleNest.Infrastructure.Persistence;
using Xunit;
using OrderEntity = StyleNest.Infrastructure.Entities.Orders.Order;
using OrderItemEntity = StyleNest.Infrastructure.Entities.Orders.OrderItem;
using SellerInventoryEntity = StyleNest.Infrastructure.Entities.Seller.SellerInventory;
using SellerEntity = StyleNest.Infrastructure.Entities.Seller.Seller;

namespace StyleNest.Admin.Tests;

public sealed class ScheduledJobTests : IDisposable
{
    private readonly AppDbContext _db;

    // Fixed clock: "now" is 2026-05-25 18:00 UTC
    // "yesterday" is 2026-05-24
    private static readonly DateTime T_Now       = new(2026, 5, 25, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T_Yesterday = T_Now.Date.AddDays(-1); // 2026-05-24 00:00
    private static readonly DateTime T_Today     = T_Now.Date;              // 2026-05-25 00:00

    public ScheduledJobTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<OrderEntity> SeedOrderAsync(
        decimal amount,
        DateTime? createdAt = null,
        OrderStatus status = OrderStatus.Confirmed,
        int itemCount = 1)
    {
        var shippingAddressId = Guid.NewGuid(); // FK not enforced in InMemory

        var order = new OrderEntity
        {
            Id                = Guid.NewGuid(),
            UserId            = Guid.NewGuid(),
            OrderNumber       = $"ORD-TEST-{Guid.NewGuid():N}"[..20],
            Status            = status,
            SubTotal          = amount,
            DiscountAmount    = 0m,
            DeliveryCharge    = 0m,
            TotalAmount       = amount,
            ShippingAddressId = shippingAddressId,
            CreatedAt         = createdAt ?? T_Yesterday.AddHours(10),
            UpdatedAt         = createdAt ?? T_Yesterday.AddHours(10),
            Items             = Enumerable.Range(0, itemCount)
                .Select(_ => new OrderItemEntity
                {
                    Id               = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    ProductName      = "Test Product",
                    VariantDetails   = "M / Black",
                    Quantity         = 1,
                    UnitPrice        = amount / itemCount,
                    TotalPrice       = amount / itemCount,
                    CreatedAt        = createdAt ?? T_Yesterday,
                    UpdatedAt        = createdAt ?? T_Yesterday,
                }).ToList(),
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private async Task<SellerInventoryEntity> SeedInventoryAsync(int stock, int threshold = 5)
    {
        // Need a seller first (FK in InMemory is not enforced, but let's be clean)
        var sellerId = Guid.NewGuid();
        _db.Sellers.Add(new SellerEntity
        {
            Id        = sellerId,
            UserId    = Guid.NewGuid(),
            StoreName = "Test Seller",
            Slug      = $"seller-{sellerId:N}",
            Status    = SellerStatus.Active,
            CreatedAt = T_Now,
            UpdatedAt = T_Now,
        });

        var inv = new SellerInventoryEntity
        {
            Id               = Guid.NewGuid(),
            SellerId         = sellerId,
            ProductVariantId = Guid.NewGuid(),
            Stock            = stock,
            Price            = 100m,
            LowStockThreshold = threshold,
            CreatedAt        = T_Now,
            UpdatedAt        = T_Now,
        };
        _db.SellerInventories.Add(inv);
        await _db.SaveChangesAsync();
        return inv;
    }

    private async Task<Cart> SeedCartWithItemAsync(DateTime? updatedAt = null)
    {
        var cartId = Guid.NewGuid();
        var cart = new Cart
        {
            Id        = cartId,
            UserId    = Guid.NewGuid(),
            CreatedAt = updatedAt ?? T_Now.AddHours(-25),
            UpdatedAt = updatedAt ?? T_Now.AddHours(-25),
        };
        _db.Carts.Add(cart);
        _db.CartItems.Add(new CartItem
        {
            Id               = Guid.NewGuid(),
            CartId           = cartId,
            ProductVariantId = Guid.NewGuid(),
            Quantity         = 1,
            CreatedAt        = cart.UpdatedAt,
            UpdatedAt        = cart.UpdatedAt,
        });
        await _db.SaveChangesAsync();
        return cart;
    }

    private async Task<Coupon> SeedCouponAsync(bool isActive, DateTime? expiresAt)
    {
        var coupon = new Coupon
        {
            Id            = Guid.NewGuid(),
            Code          = $"TEST{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            Description   = "Test coupon",
            DiscountType  = DiscountType.FlatAmount,
            DiscountValue = 50m,
            IsActive      = isActive,
            ExpiresAt     = expiresAt,
            CreatedAt     = T_Now.AddDays(-10),
            UpdatedAt     = T_Now.AddDays(-10),
        };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    // ═══════════════════ DailyAnalyticsJob ═══════════════════════════════════

    // ── TC-ADMIN-JOB-001: creates DailyRevenue row for yesterday ─────────────

    [Fact]
    public async Task DailyAnalytics_YesterdayOrder_CreatesRevenueRow()
    {
        await SeedOrderAsync(1500m, createdAt: T_Yesterday.AddHours(9));

        var job    = new DailyAnalyticsJob(_db);
        var result = await job.ExecuteAsync(T_Now);

        result.JobName.Should().Be("DailyAnalyticsJob");
        result.ItemsAffected.Should().Be(1);

        var row = await _db.DailyRevenues.SingleAsync();
        row.Date.Should().Be(T_Yesterday,
            because: "TC-ADMIN-JOB-001: the row must target yesterday's date");
        row.Revenue.Should().Be(1500m);
        row.OrderCount.Should().Be(1);
    }

    // ── TC-ADMIN-JOB-002: cancelled orders excluded from revenue ─────────────

    [Fact]
    public async Task DailyAnalytics_CancelledOrdersExcluded()
    {
        await SeedOrderAsync(2000m, createdAt: T_Yesterday.AddHours(9), status: OrderStatus.Cancelled);

        var result = await new DailyAnalyticsJob(_db).ExecuteAsync(T_Now);

        var row = await _db.DailyRevenues.SingleAsync();
        row.Revenue.Should().Be(0m,
            because: "TC-ADMIN-JOB-002: cancelled orders must not contribute to revenue");
        row.OrderCount.Should().Be(0);
    }

    // ── TC-ADMIN-JOB-003: today's orders not included ────────────────────────

    [Fact]
    public async Task DailyAnalytics_TodayOrdersExcluded()
    {
        // Order created today (not yesterday)
        await SeedOrderAsync(999m, createdAt: T_Today.AddHours(1));

        await new DailyAnalyticsJob(_db).ExecuteAsync(T_Now);

        var row = await _db.DailyRevenues.SingleAsync();
        row.Revenue.Should().Be(0m,
            because: "TC-ADMIN-JOB-003: only yesterday's orders count; today's orders must be excluded");
        row.OrderCount.Should().Be(0);
    }

    // ── TC-ADMIN-JOB-004: idempotent upsert ──────────────────────────────────

    [Fact]
    public async Task DailyAnalytics_RunTwice_UpdatesRowNoDuplicate()
    {
        await SeedOrderAsync(800m, createdAt: T_Yesterday.AddHours(10));

        var job = new DailyAnalyticsJob(_db);
        await job.ExecuteAsync(T_Now);
        await job.ExecuteAsync(T_Now); // second run — must update, not insert

        var rows = await _db.DailyRevenues.ToListAsync();
        rows.Should().HaveCount(1,
            because: "TC-ADMIN-JOB-004: re-running the job for the same date must upsert, not duplicate");
    }

    // ── TC-ADMIN-JOB-005: no orders → zero-value row created ─────────────────

    [Fact]
    public async Task DailyAnalytics_NoOrders_CreatesZeroRow()
    {
        // No orders seeded at all

        await new DailyAnalyticsJob(_db).ExecuteAsync(T_Now);

        var row = await _db.DailyRevenues.SingleAsync();
        row.Revenue.Should().Be(0m,
            because: "TC-ADMIN-JOB-005: a zero-revenue row must still be written when there are no orders");
        row.OrderCount.Should().Be(0);
        row.ItemCount.Should().Be(0);
    }

    // ═══════════════════ LowStockAlertJob ═══════════════════════════════════

    // ── TC-ADMIN-JOB-006: items below threshold are counted ──────────────────

    [Fact]
    public async Task LowStock_ItemsBelowThreshold_Counted()
    {
        await SeedInventoryAsync(stock: 3, threshold: 5);  // 3 ≤ 5 → low stock
        await SeedInventoryAsync(stock: 2, threshold: 5);  // 2 ≤ 5 → low stock

        var result = await new LowStockAlertJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(2,
            because: "TC-ADMIN-JOB-006: both items below threshold must be counted");
    }

    // ── TC-ADMIN-JOB-007: items above threshold excluded ─────────────────────

    [Fact]
    public async Task LowStock_ItemsAboveThreshold_NotCounted()
    {
        await SeedInventoryAsync(stock: 20, threshold: 5); // 20 > 5 → NOT low stock

        var result = await new LowStockAlertJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(0,
            because: "TC-ADMIN-JOB-007: items with stock well above threshold must not trigger alerts");
    }

    // ── TC-ADMIN-JOB-008: zero-stock items excluded (OOS is separate concern) ─

    [Fact]
    public async Task LowStock_ZeroStockItems_NotCountedAsLowStock()
    {
        await SeedInventoryAsync(stock: 0, threshold: 5); // 0 ≤ 5 but Stock=0 → excluded

        var result = await new LowStockAlertJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(0,
            because: "TC-ADMIN-JOB-008: completely out-of-stock items are a separate concern from low-stock alerts");
    }

    // ═══════════════════ CartAbandonmentJob ══════════════════════════════════

    // ── TC-ADMIN-JOB-009: idle cart with items is counted ────────────────────

    [Fact]
    public async Task CartAbandonment_IdleCartWithItems_Counted()
    {
        await SeedCartWithItemAsync(updatedAt: T_Now.AddHours(-25)); // idle > 24h

        var result = await new CartAbandonmentJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(1,
            because: "TC-ADMIN-JOB-009: a cart idle for more than 24 hours with items must be counted as abandoned");
    }

    // ── TC-ADMIN-JOB-010: recently active cart not counted ───────────────────

    [Fact]
    public async Task CartAbandonment_RecentCart_NotCounted()
    {
        await SeedCartWithItemAsync(updatedAt: T_Now.AddHours(-2)); // only 2h idle

        var result = await new CartAbandonmentJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(0,
            because: "TC-ADMIN-JOB-010: a cart updated within the last 24 hours must not be classified as abandoned");
    }

    // ── TC-ADMIN-JOB-011: empty cart (no items) not counted ──────────────────

    [Fact]
    public async Task CartAbandonment_EmptyCart_NotCounted()
    {
        // Cart with no items
        _db.Carts.Add(new Cart
        {
            Id        = Guid.NewGuid(),
            UserId    = Guid.NewGuid(),
            CreatedAt = T_Now.AddHours(-48),
            UpdatedAt = T_Now.AddHours(-48), // old enough
        });
        await _db.SaveChangesAsync();

        var result = await new CartAbandonmentJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(0,
            because: "TC-ADMIN-JOB-011: empty carts (no CartItems) must not be counted as abandoned");
    }

    // ═══════════════════ ExpireCouponsJob ════════════════════════════════════

    // ── TC-ADMIN-JOB-012: expired coupon is deactivated ──────────────────────

    [Fact]
    public async Task ExpireCoupons_ExpiredActiveCoupon_IsDeactivated()
    {
        var coupon = await SeedCouponAsync(isActive: true, expiresAt: T_Now.AddDays(-1));

        await new ExpireCouponsJob(_db).ExecuteAsync(T_Now);

        var fromDb = await _db.Coupons.FindAsync(coupon.Id);
        fromDb!.IsActive.Should().BeFalse(
            because: "TC-ADMIN-JOB-012: a coupon past its ExpiresAt must be set to IsActive=false");
    }

    // ── TC-ADMIN-JOB-013: non-expired coupon untouched ───────────────────────

    [Fact]
    public async Task ExpireCoupons_NonExpiredCoupon_Untouched()
    {
        var coupon = await SeedCouponAsync(isActive: true, expiresAt: T_Now.AddDays(7));

        await new ExpireCouponsJob(_db).ExecuteAsync(T_Now);

        var fromDb = await _db.Coupons.FindAsync(coupon.Id);
        fromDb!.IsActive.Should().BeTrue(
            because: "TC-ADMIN-JOB-013: a coupon that has not yet expired must remain active");
    }

    // ── TC-ADMIN-JOB-014: returns correct count of deactivated coupons ───────

    [Fact]
    public async Task ExpireCoupons_ReturnsCorrectCount()
    {
        await SeedCouponAsync(isActive: true, expiresAt: T_Now.AddDays(-3));  // expired
        await SeedCouponAsync(isActive: true, expiresAt: T_Now.AddDays(-1));  // expired
        await SeedCouponAsync(isActive: true, expiresAt: T_Now.AddDays(+5));  // still valid
        await SeedCouponAsync(isActive: false, expiresAt: T_Now.AddDays(-1)); // already inactive

        var result = await new ExpireCouponsJob(_db).ExecuteAsync(T_Now);

        result.ItemsAffected.Should().Be(2,
            because: "TC-ADMIN-JOB-014: only the two currently-active expired coupons must be counted");
    }
}
