/**
 * ENH-PDP-006 — Back-in-Stock Notification: BackInStockService tests
 *
 * Acceptance criteria (FR-PDP-012):
 *   TC-PDP-006-01: Subscribe — creates subscription with correct fields
 *   TC-PDP-006-02: Subscribe — idempotent: second subscribe returns existing row, no duplicate
 *   TC-PDP-006-03: Subscribe — stores email lowercased and trimmed
 *   TC-PDP-006-04: Subscribe — stores optional phone and variantId
 *   TC-PDP-006-05: Unsubscribe — soft-deletes existing subscription
 *   TC-PDP-006-06: Unsubscribe — idempotent: no throw when subscription not found
 *   TC-PDP-006-07: GetSubscription — returns dto when active subscription exists
 *   TC-PDP-006-08: GetSubscription — returns null when no subscription
 *   TC-PDP-006-09: NotifySubscribers — marks NotifiedAt on all pending subscriptions
 *   TC-PDP-006-10: NotifySubscribers — enqueues NotificationOutbox entries (email per subscriber)
 *   TC-PDP-006-11: NotifySubscribers — enqueues SMS outbox entry when phone is present
 *   TC-PDP-006-12: NotifySubscribers — no outbox entries when no pending subscriptions
 *   TC-PDP-006-13: NotifySubscribers — already-notified subscriptions not re-notified
 *   TC-PDP-006-14: GetSubscription — returns null after unsubscribe
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Catalog.API.Services;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Catalog.Tests;

public sealed class BackInStockServiceTests : IDisposable
{
    private readonly AppDbContext      _db;
    private readonly BackInStockService _svc;

    private static readonly Guid ProductId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid UserId1   = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001");
    private static readonly Guid UserId2   = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000002");
    private static readonly Guid VariantId = Guid.Parse("DDDDDDDD-0000-0000-0000-000000000001");

    public BackInStockServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _svc = new BackInStockService(_db);
        SeedProduct();
    }

    public void Dispose() => _db.Dispose();

    // ─── Subscribe ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-006-01: Subscribe — creates subscription with correct fields")]
    public async Task Subscribe_CreatesSubscription()
    {
        var dto = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("User@Example.COM", "+919876543210", VariantId));

        dto.UserId.Should().Be(UserId1);
        dto.ProductId.Should().Be(ProductId);
        dto.VariantId.Should().Be(VariantId);
        dto.NotifiedAt.Should().BeNull();
    }

    [Fact(DisplayName = "TC-PDP-006-02: Subscribe — idempotent: second subscribe returns same row")]
    public async Task Subscribe_Idempotent_ReturnsSameRow()
    {
        var dto1 = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("a@b.com"));
        var dto2 = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("a@b.com"));

        dto1.Id.Should().Be(dto2.Id);
        _db.BackInStockSubscriptions.IgnoreQueryFilters()
           .Count(s => s.UserId == UserId1 && s.ProductId == ProductId)
           .Should().Be(1);
    }

    [Fact(DisplayName = "TC-PDP-006-03: Subscribe — stores email lowercased and trimmed")]
    public async Task Subscribe_NormalisesEmail()
    {
        var dto = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("  SHOPPER@EXAMPLE.COM  "));

        dto.Email.Should().Be("shopper@example.com");
    }

    [Fact(DisplayName = "TC-PDP-006-04: Subscribe — stores optional phone and variantId")]
    public async Task Subscribe_StoresPhoneAndVariantId()
    {
        var dto = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("x@y.com", "+911234567890", VariantId));

        dto.Phone.Should().Be("+911234567890");
        dto.VariantId.Should().Be(VariantId);
    }

    // ─── Unsubscribe ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-006-05: Unsubscribe — soft-deletes existing subscription")]
    public async Task Unsubscribe_SoftDeletes()
    {
        await _svc.SubscribeAsync(ProductId, UserId1, new SubscribeBackInStockRequest("a@b.com"));
        await _svc.UnsubscribeAsync(ProductId, UserId1, null);

        var stored = await _db.BackInStockSubscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == UserId1 && s.ProductId == ProductId);

        stored.Should().NotBeNull();
        stored!.IsDeleted.Should().BeTrue();
    }

    [Fact(DisplayName = "TC-PDP-006-06: Unsubscribe — idempotent: no throw when not found")]
    public async Task Unsubscribe_Idempotent_NoThrow()
    {
        var act = async () => await _svc.UnsubscribeAsync(ProductId, UserId1, null);
        await act.Should().NotThrowAsync();
    }

    // ─── GetSubscription ──────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-006-07: GetSubscription — returns dto when active")]
    public async Task GetSubscription_ReturnsDto_WhenActive()
    {
        var created = await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("a@b.com"));

        var dto = await _svc.GetSubscriptionAsync(ProductId, UserId1, null);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(created.Id);
    }

    [Fact(DisplayName = "TC-PDP-006-08: GetSubscription — returns null when no subscription")]
    public async Task GetSubscription_ReturnsNull_WhenNone()
    {
        var dto = await _svc.GetSubscriptionAsync(ProductId, UserId1, null);
        dto.Should().BeNull();
    }

    // ─── NotifySubscribers ────────────────────────────────────────────────────

    [Fact(DisplayName = "TC-PDP-006-09: NotifySubscribers — marks NotifiedAt on all pending")]
    public async Task Notify_SetsNotifiedAt()
    {
        await _svc.SubscribeAsync(ProductId, UserId1, new SubscribeBackInStockRequest("a@b.com"));
        await _svc.SubscribeAsync(ProductId, UserId2, new SubscribeBackInStockRequest("c@d.com"));

        var result = await _svc.NotifySubscribersAsync(ProductId);

        result.SubscribersNotified.Should().Be(2);

        var subs = await _db.BackInStockSubscriptions.IgnoreQueryFilters()
            .Where(s => s.ProductId == ProductId).ToListAsync();
        subs.Should().AllSatisfy(s => s.NotifiedAt.Should().NotBeNull());
    }

    [Fact(DisplayName = "TC-PDP-006-10: NotifySubscribers — enqueues email outbox entry per subscriber")]
    public async Task Notify_EnqueuesEmailOutbox()
    {
        await _svc.SubscribeAsync(ProductId, UserId1, new SubscribeBackInStockRequest("a@b.com"));
        await _svc.SubscribeAsync(ProductId, UserId2, new SubscribeBackInStockRequest("c@d.com"));

        await _svc.NotifySubscribersAsync(ProductId);

        var emailEntries = await _db.NotificationOutbox
            .IgnoreQueryFilters()
            .Where(n => n.Type == "BackInStock.Email")
            .ToListAsync();

        emailEntries.Should().HaveCount(2);
    }

    [Fact(DisplayName = "TC-PDP-006-11: NotifySubscribers — enqueues SMS outbox when phone present")]
    public async Task Notify_EnqueuesSmsWhenPhonePresent()
    {
        await _svc.SubscribeAsync(ProductId, UserId1,
            new SubscribeBackInStockRequest("a@b.com", "+911234567890"));
        await _svc.SubscribeAsync(ProductId, UserId2,
            new SubscribeBackInStockRequest("c@d.com")); // no phone

        await _svc.NotifySubscribersAsync(ProductId);

        var smsEntries = await _db.NotificationOutbox
            .IgnoreQueryFilters()
            .Where(n => n.Type == "BackInStock.SMS")
            .ToListAsync();

        smsEntries.Should().HaveCount(1); // only UserId1 had a phone
    }

    [Fact(DisplayName = "TC-PDP-006-12: NotifySubscribers — returns 0 when no pending subscribers")]
    public async Task Notify_ReturnsZero_WhenNoPending()
    {
        var result = await _svc.NotifySubscribersAsync(ProductId);
        result.SubscribersNotified.Should().Be(0);

        var outbox = await _db.NotificationOutbox.IgnoreQueryFilters().ToListAsync();
        outbox.Should().BeEmpty();
    }

    [Fact(DisplayName = "TC-PDP-006-13: NotifySubscribers — already-notified subscriptions not re-notified")]
    public async Task Notify_SkipsAlreadyNotified()
    {
        await _svc.SubscribeAsync(ProductId, UserId1, new SubscribeBackInStockRequest("a@b.com"));

        // First run — notifies
        await _svc.NotifySubscribersAsync(ProductId);

        // Second run — nothing pending
        var result2 = await _svc.NotifySubscribersAsync(ProductId);
        result2.SubscribersNotified.Should().Be(0);

        // Only one email outbox entry total
        var emailCount = await _db.NotificationOutbox.IgnoreQueryFilters()
            .CountAsync(n => n.Type == "BackInStock.Email");
        emailCount.Should().Be(1);
    }

    [Fact(DisplayName = "TC-PDP-006-14: GetSubscription — returns null after unsubscribe")]
    public async Task GetSubscription_Null_AfterUnsubscribe()
    {
        await _svc.SubscribeAsync(ProductId, UserId1, new SubscribeBackInStockRequest("a@b.com"));
        await _svc.UnsubscribeAsync(ProductId, UserId1, null);

        var dto = await _svc.GetSubscriptionAsync(ProductId, UserId1, null);
        dto.Should().BeNull();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SeedProduct()
    {
        var cat = new Category
        {
            Id = Guid.Parse("CCCCCCCC-0000-0000-0000-000000000001"),
            Name = "Cat", Slug = "cat",
        };
        var brand = new Brand
        {
            Id = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000001"),
            Name = "Brand", Slug = "brand",
        };
        var product = new Product
        {
            Id         = ProductId,
            Name       = "Test Product",
            Slug       = "test-product",
            BasePrice  = 999m,
            CategoryId = cat.Id,
            BrandId    = brand.Id,
            IsActive   = true,
        };
        _db.Categories.Add(cat);
        _db.Brands.Add(brand);
        _db.Products.Add(product);
        _db.SaveChanges();
    }
}
