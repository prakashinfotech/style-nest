/**
 * ENH-CHKOUT-002 — Express Checkout (one-tap with saved address + saved payment)
 * Source: FR-CHKOUT (TSD §5)
 *
 * Acceptance criteria tested here:
 *
 *   TC-CHKOUT-002-01: GetPreview → default address + non-expired card → CanExpressCheckout=true
 *   TC-CHKOUT-002-02: GetPreview → no default address → CanExpressCheckout=false, BlockReason mentions "address"
 *   TC-CHKOUT-002-03: GetPreview → no default card saved → CanExpressCheckout=false, BlockReason mentions "card"
 *   TC-CHKOUT-002-04: GetPreview → default card is expired → CanExpressCheckout=false
 *   TC-CHKOUT-002-05: GetPreview → default card is soft-deleted → treated as absent → CanExpressCheckout=false
 *   TC-CHKOUT-002-06: GetPreview → DefaultAddress populated with correct fields when eligible
 *   TC-CHKOUT-002-07: GetPreview → DefaultCard populated with correct Last4/Network when eligible
 *   TC-CHKOUT-002-08: PlaceExpressOrder → no default address → throws InvalidOperationException
 *   TC-CHKOUT-002-09: PlaceExpressOrder → no default card → throws InvalidOperationException
 *   TC-CHKOUT-002-10: PlaceExpressOrder → expired card → throws InvalidOperationException
 *   TC-CHKOUT-002-11: PlaceExpressOrder → deleted card → throws InvalidOperationException
 *   TC-CHKOUT-002-12: PlaceExpressOrder → success → delegates to IOrderService with address fields
 *   TC-CHKOUT-002-13: PlaceExpressOrder → coupon code passed through to IOrderService
 *   TC-CHKOUT-002-14: GetPreview → non-default address does not satisfy eligibility
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.DTOs;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class ExpressCheckoutServiceTests : IDisposable
{
    private readonly AppDbContext     _db;
    private readonly FakeOrderService _fakeOrder;
    private readonly ExpressCheckoutService _sut;

    private readonly Guid _userId = Guid.NewGuid();

    private static readonly DateTime T0 = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    public ExpressCheckoutServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db        = new AppDbContext(opts);
        _fakeOrder = new FakeOrderService();
        _sut       = new ExpressCheckoutService(_db, _fakeOrder);
    }

    public void Dispose() => _db.Dispose();

    // ── Fake ─────────────────────────────────────────────────────────────────

    private sealed class FakeOrderService : IOrderService
    {
        public PlaceOrderRequest? LastRequest { get; private set; }

        public Task<OrderDto> PlaceOrderAsync(
            Guid userId, PlaceOrderRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new OrderDto(
                Guid.NewGuid(), "ORD-20260525-TEST", "Pending",
                500m, 0m, 49m, 549m, request.CouponCode,
                DateTime.UtcNow, []));
        }

        public Task<OrderDto> BuyNowAsync(
            Guid userId, BuyNowRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
            Guid userId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<OrderDto?> GetOrderAsync(
            Guid userId, Guid orderId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task CancelOrderAsync(
            Guid userId, Guid orderId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<UserAddress> SeedAddressAsync(bool isDefault = true)
    {
        var address = new UserAddress
        {
            Id           = Guid.NewGuid(),
            UserId       = _userId,
            Label        = "Home",
            RecipientName = "Test User",
            PhoneNumber  = "9876543210",
            AddressLine1 = "42 Main Street",
            AddressLine2 = "Apt 2B",
            City         = "Mumbai",
            State        = "Maharashtra",
            PinCode      = "400001",
            IsDefault    = isDefault,
            CreatedAt    = T0,
            UpdatedAt    = T0,
        };
        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync();
        return address;
    }

    private async Task<CardToken> SeedCardAsync(
        bool isDefault  = true,
        bool isDeleted  = false,
        int  expiryYear = 2030,
        int  expiryMonth = 12)
    {
        var token = new CardToken
        {
            Id                 = Guid.NewGuid(),
            UserId             = _userId,
            RazorpayTokenId    = $"token_{Guid.NewGuid():N}",
            RazorpayCustomerId = "cust_TEST",
            Last4              = "4242",
            Network            = CardNetwork.Visa,
            ExpiryMonth        = expiryMonth,
            ExpiryYear         = expiryYear,
            CardholderName     = "Test User",
            IsDefault          = isDefault,
            IsDeleted          = isDeleted,
            CreatedAt          = T0,
            UpdatedAt          = T0,
        };
        _db.CardTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    // ── TC-CHKOUT-002-01: both defaults present → eligible ───────────────────

    [Fact]
    public async Task GetPreview_DefaultAddressAndValidCard_CanExpressCheckoutTrue()
    {
        await SeedAddressAsync();
        await SeedCardAsync();

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeTrue(
            because: "TC-CHKOUT-002-01: a user with both a default address and a non-expired default card must be eligible");
        preview.BlockReason.Should().BeNull();
    }

    // ── TC-CHKOUT-002-02: no default address → blocked ───────────────────────

    [Fact]
    public async Task GetPreview_NoDefaultAddress_BlocksWithAddressMessage()
    {
        // Seed a non-default address — must not satisfy the requirement
        await SeedAddressAsync(isDefault: false);
        await SeedCardAsync();

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeFalse(
            because: "TC-CHKOUT-002-02: a non-default address must not count as an express-checkout address");
        preview.BlockReason.Should().ContainEquivalentOf("address",
            because: "TC-CHKOUT-002-02: block reason must guide the user to add a default address");
        preview.DefaultAddress.Should().BeNull();
    }

    // ── TC-CHKOUT-002-03: no card saved → blocked ────────────────────────────

    [Fact]
    public async Task GetPreview_NoDefaultCard_BlocksWithCardMessage()
    {
        await SeedAddressAsync();
        // no card seeded

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeFalse(
            because: "TC-CHKOUT-002-03: a user without a default card cannot use express checkout");
        preview.BlockReason.Should().ContainEquivalentOf("card",
            because: "TC-CHKOUT-002-03: block reason must guide the user to save a card");
        preview.DefaultAddress.Should().NotBeNull(
            because: "TC-CHKOUT-002-03: default address IS present, so it should be returned in the preview");
    }

    // ── TC-CHKOUT-002-04: expired card → blocked ─────────────────────────────

    [Fact]
    public async Task GetPreview_ExpiredCard_BlocksWithExpiryMessage()
    {
        await SeedAddressAsync();
        // Expired: year 2024, month 1 — clearly before T0 (2026-05-25)
        await SeedCardAsync(expiryYear: 2024, expiryMonth: 1);

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeFalse(
            because: "TC-CHKOUT-002-04: an expired default card must block express checkout");
        preview.BlockReason.Should().ContainEquivalentOf("expired",
            because: "TC-CHKOUT-002-04: the user must be told their card has expired");
        preview.DefaultCard.Should().NotBeNull("the expired card metadata is still returned for display");
        preview.DefaultCard!.IsExpired.Should().BeTrue();
    }

    // ── TC-CHKOUT-002-05: soft-deleted card → treated as absent ──────────────

    [Fact]
    public async Task GetPreview_SoftDeletedDefaultCard_TreatedAsAbsent()
    {
        await SeedAddressAsync();
        await SeedCardAsync(isDeleted: true);

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeFalse(
            because: "TC-CHKOUT-002-05: a soft-deleted card (IsDeleted=true) must be ignored");
        preview.DefaultCard.Should().BeNull(
            because: "deleted cards must not appear in the preview response");
    }

    // ── TC-CHKOUT-002-06: preview populates DefaultAddress correctly ──────────

    [Fact]
    public async Task GetPreview_DefaultAddress_IsMappedCorrectly()
    {
        var address = await SeedAddressAsync();
        await SeedCardAsync();

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.DefaultAddress.Should().NotBeNull();
        preview.DefaultAddress!.Id.Should().Be(address.Id,
            because: "TC-CHKOUT-002-06: the preview must identify which address will be used");
        preview.DefaultAddress.AddressLine1.Should().Be(address.AddressLine1);
        preview.DefaultAddress.City.Should().Be(address.City);
        preview.DefaultAddress.PinCode.Should().Be(address.PinCode);
    }

    // ── TC-CHKOUT-002-07: preview populates DefaultCard correctly ─────────────

    [Fact]
    public async Task GetPreview_DefaultCard_IsMappedCorrectly()
    {
        await SeedAddressAsync();
        var card = await SeedCardAsync();

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.DefaultCard.Should().NotBeNull();
        preview.DefaultCard!.Id.Should().Be(card.Id,
            because: "TC-CHKOUT-002-07: the preview must identify which card will be charged");
        preview.DefaultCard.Last4.Should().Be("4242");
        preview.DefaultCard.Network.Should().Be(CardNetwork.Visa);
        preview.DefaultCard.IsExpired.Should().BeFalse();
    }

    // ── TC-CHKOUT-002-08: PlaceExpressOrder → no address → throws ────────────

    [Fact]
    public async Task PlaceExpressOrder_NoDefaultAddress_ThrowsInvalidOperation()
    {
        await SeedCardAsync();
        // No address seeded

        Func<Task> act = () => _sut.PlaceExpressOrderAsync(_userId);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-CHKOUT-002-08: express checkout without a default address must throw")
            .WithMessage("*address*");
    }

    // ── TC-CHKOUT-002-09: PlaceExpressOrder → no card → throws ───────────────

    [Fact]
    public async Task PlaceExpressOrder_NoDefaultCard_ThrowsInvalidOperation()
    {
        await SeedAddressAsync();
        // No card seeded

        Func<Task> act = () => _sut.PlaceExpressOrderAsync(_userId);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-CHKOUT-002-09: express checkout without a saved card must throw")
            .WithMessage("*card*");
    }

    // ── TC-CHKOUT-002-10: PlaceExpressOrder → expired card → throws ──────────

    [Fact]
    public async Task PlaceExpressOrder_ExpiredCard_ThrowsInvalidOperation()
    {
        await SeedAddressAsync();
        await SeedCardAsync(expiryYear: 2020, expiryMonth: 6);

        Func<Task> act = () => _sut.PlaceExpressOrderAsync(_userId);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-CHKOUT-002-10: express checkout with an expired card must throw")
            .WithMessage("*expired*");
    }

    // ── TC-CHKOUT-002-11: PlaceExpressOrder → deleted card → throws ──────────

    [Fact]
    public async Task PlaceExpressOrder_SoftDeletedCard_ThrowsInvalidOperation()
    {
        await SeedAddressAsync();
        await SeedCardAsync(isDeleted: true);

        Func<Task> act = () => _sut.PlaceExpressOrderAsync(_userId);

        await act.Should().ThrowAsync<InvalidOperationException>(
            because: "TC-CHKOUT-002-11: a soft-deleted card must not be usable for express checkout")
            .WithMessage("*card*");
    }

    // ── TC-CHKOUT-002-12: PlaceExpressOrder success → address fields forwarded ─

    [Fact]
    public async Task PlaceExpressOrder_Success_ForwardsDefaultAddressToOrderService()
    {
        var address = await SeedAddressAsync();
        await SeedCardAsync();

        var order = await _sut.PlaceExpressOrderAsync(_userId);

        order.Should().NotBeNull(
            because: "TC-CHKOUT-002-12: a successful express checkout must return an OrderDto");
        _fakeOrder.LastRequest.Should().NotBeNull();
        _fakeOrder.LastRequest!.AddressLine1.Should().Be(address.AddressLine1,
            because: "TC-CHKOUT-002-12: the PlaceOrderRequest must use the default address line 1");
        _fakeOrder.LastRequest.City.Should().Be(address.City);
        _fakeOrder.LastRequest.State.Should().Be(address.State);
        _fakeOrder.LastRequest.Pincode.Should().Be(address.PinCode);
        _fakeOrder.LastRequest.PaymentMethod.Should().Be("SAVED_CARD",
            because: "TC-CHKOUT-002-12: express checkout must signal a saved-card payment method");
    }

    // ── TC-CHKOUT-002-13: coupon code passed through ──────────────────────────

    [Fact]
    public async Task PlaceExpressOrder_CouponCode_PassedThroughToOrderService()
    {
        await SeedAddressAsync();
        await SeedCardAsync();

        await _sut.PlaceExpressOrderAsync(_userId, couponCode: "SUMMER20");

        _fakeOrder.LastRequest.Should().NotBeNull();
        _fakeOrder.LastRequest!.CouponCode.Should().Be("SUMMER20",
            because: "TC-CHKOUT-002-13: coupon code must be forwarded verbatim to IOrderService");
    }

    // ── TC-CHKOUT-002-14: non-default address does not qualify ────────────────

    [Fact]
    public async Task GetPreview_NonDefaultAddress_DoesNotSatisfyEligibility()
    {
        // Only a non-default address exists; no default address
        await SeedAddressAsync(isDefault: false);
        await SeedCardAsync();

        var preview = await _sut.GetPreviewAsync(_userId);

        preview.CanExpressCheckout.Should().BeFalse(
            because: "TC-CHKOUT-002-14: non-default addresses must not be used for express checkout");
        preview.DefaultAddress.Should().BeNull();
    }
}
