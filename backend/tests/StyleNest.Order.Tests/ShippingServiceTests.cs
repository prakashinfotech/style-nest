/**
 * ENH-ORD-005 — Shiprocket / Delhivery AWB + Tracking + NDR Integration
 * Acceptance criteria tested here:
 *   - CreateShipmentAsync: assigns AWB + carrier to order, creates PICKED_UP tracking event
 *   - GetTrackingAsync: returns all events ordered by OccurredAt (oldest first)
 *   - HandleNdrAsync: appends NDR event with reason code
 *   - AddTrackingEventAsync: appends arbitrary carrier event
 *   - Unknown orderId → GetTrackingAsync returns null
 *   - MockShippingProviderClient: generates deterministic AWB (provider-prefixed)
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class ShippingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IShippingProviderClient> _providerMock;
    private readonly ShippingSettings _settings;

    public ShippingServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db           = new AppDbContext(opts);
        _providerMock = new Mock<IShippingProviderClient>();
        _settings     = new ShippingSettings { Provider = "SHIPROCKET" };
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private ShippingService BuildSut() =>
        new(_db, _providerMock.Object,
            Options.Create(_settings),
            NullLogger<ShippingService>.Instance);

    private async Task<Infrastructure.Entities.Orders.Order> SeedOrderAsync(string orderNumber = "TC-001")
    {
        var order = new Infrastructure.Entities.Orders.Order
        {
            Id          = Guid.NewGuid(),
            OrderNumber = orderNumber,
            UserId      = Guid.NewGuid(),
            Status      = OrderStatus.Confirmed,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ── CreateShipmentAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateShipment_AssignsAwbToOrder()
    {
        var order = await SeedOrderAsync("TC-100");
        _providerMock
            .Setup(p => p.CreateAwbAsync(order.Id, order.OrderNumber, default))
            .ReturnsAsync("SRT-ABC123");

        await BuildSut().CreateShipmentAsync(order.Id);

        _db.Entry(order).Reload();
        order.AwbNumber.Should().Be("SRT-ABC123");
    }

    [Fact]
    public async Task CreateShipment_AssignsCarrierName()
    {
        var order = await SeedOrderAsync("TC-101");
        _providerMock
            .Setup(p => p.CreateAwbAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync("SRT-XYZ");

        await BuildSut().CreateShipmentAsync(order.Id);

        _db.Entry(order).Reload();
        order.CarrierName.Should().Be("SHIPROCKET");
    }

    [Fact]
    public async Task CreateShipment_CreatesPickedUpTrackingEvent()
    {
        var order = await SeedOrderAsync("TC-102");
        _providerMock
            .Setup(p => p.CreateAwbAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync("SRT-EVT");

        await BuildSut().CreateShipmentAsync(order.Id);

        var events = await _db.ShipmentTrackings
            .IgnoreQueryFilters()
            .Where(t => t.OrderId == order.Id)
            .ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("PICKED_UP");
    }

    [Fact]
    public async Task CreateShipment_UnknownOrder_DoesNotThrow()
    {
        _providerMock
            .Setup(p => p.CreateAwbAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync("MOCK-X");

        var act = async () => await BuildSut().CreateShipmentAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    // ── GetTrackingAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTracking_ReturnsEventsInChronologicalOrder()
    {
        var order = await SeedOrderAsync("TC-200");
        var t1    = DateTime.UtcNow.AddHours(-3);
        var t2    = DateTime.UtcNow.AddHours(-1);

        _db.ShipmentTrackings.AddRange(
            new ShipmentTracking { Id = Guid.NewGuid(), OrderId = order.Id, EventType = "IN_TRANSIT",        Description = "At hub",  OccurredAt = t1 },
            new ShipmentTracking { Id = Guid.NewGuid(), OrderId = order.Id, EventType = "OUT_FOR_DELIVERY",  Description = "En route", OccurredAt = t2 });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetTrackingAsync(order.Id);

        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(2);
        result.Events[0].EventType.Should().Be("IN_TRANSIT",       "oldest event first");
        result.Events[1].EventType.Should().Be("OUT_FOR_DELIVERY", "newest event last");
    }

    [Fact]
    public async Task GetTracking_ReturnsAwbAndCarrierFromOrder()
    {
        var order = await SeedOrderAsync("TC-201");
        order.AwbNumber   = "SRT-TRACKED";
        order.CarrierName = "SHIPROCKET";
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetTrackingAsync(order.Id);

        result!.AwbNumber.Should().Be("SRT-TRACKED");
        result.CarrierName.Should().Be("SHIPROCKET");
    }

    [Fact]
    public async Task GetTracking_UnknownOrderId_ReturnsNull()
    {
        var result = await BuildSut().GetTrackingAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── HandleNdrAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleNdr_CreatesNdrTrackingEvent()
    {
        var order = await SeedOrderAsync("TC-300");

        await BuildSut().HandleNdrAsync(order.Id, "CUSTOMER_UNAVAILABLE", "Mumbai", default);

        var events = await _db.ShipmentTrackings
            .IgnoreQueryFilters()
            .Where(t => t.OrderId == order.Id)
            .ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("NDR");
        events[0].NdrReason.Should().Be("CUSTOMER_UNAVAILABLE");
        events[0].Location.Should().Be("Mumbai");
    }

    [Fact]
    public async Task HandleNdr_UnknownOrder_DoesNotThrow()
    {
        var act = async () => await BuildSut().HandleNdrAsync(Guid.NewGuid(), "ADDRESS_NOT_FOUND", null, default);

        await act.Should().NotThrowAsync();
    }

    // ── AddTrackingEventAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task AddTrackingEvent_AppendsEventWithCorrectFields()
    {
        var order     = await SeedOrderAsync("TC-400");
        var occurredAt = DateTime.UtcNow.AddMinutes(-10);

        await BuildSut().AddTrackingEventAsync(
            order.Id, "IN_TRANSIT", "Arrived at Mumbai hub", "Mumbai Hub", occurredAt, default);

        var events = await _db.ShipmentTrackings
            .IgnoreQueryFilters()
            .Where(t => t.OrderId == order.Id)
            .ToListAsync();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("IN_TRANSIT");
        events[0].Description.Should().Be("Arrived at Mumbai hub");
        events[0].Location.Should().Be("Mumbai Hub");
        events[0].OccurredAt.Should().BeCloseTo(occurredAt, TimeSpan.FromSeconds(1));
    }

    // ── MockShippingProviderClient ────────────────────────────────────────────

    [Theory]
    [InlineData("SHIPROCKET", "SRT")]
    [InlineData("DELHIVERY",  "DEL")]
    [InlineData("MOCK",       "MOCK")]
    public async Task MockProvider_GeneratesCorrectPrefix(string provider, string expectedPrefix)
    {
        var settings = new ShippingSettings { Provider = provider };
        var client   = new MockShippingProviderClient(Options.Create(settings));

        var awb = await client.CreateAwbAsync(Guid.NewGuid(), "TC-001");

        awb.Should().StartWith(expectedPrefix + "-");
    }
}
