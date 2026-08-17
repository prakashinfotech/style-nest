/**
 * ENH-CHKOUT-002 — Express Checkout (one-tap with saved address + saved payment)
 * Source: FR-CHKOUT (TSD §5)
 *
 * Allows a logged-in user to skip the checkout form entirely by pre-filling:
 *   - ShippingAddress: the user's default saved address (IsDefault=true, !IsDeleted)
 *   - PaymentMethod:   the user's default non-expired saved card token (IsDefault=true, !IsDeleted)
 *
 * Two-step UX:
 *   1. GET /api/v1/checkout/express  → preview (what will be used + eligibility check)
 *   2. POST /api/v1/checkout/express → one-tap confirmation; delegates to IOrderService
 *
 * Precondition errors (all thrown as InvalidOperationException):
 *   - No default address saved
 *   - No default card saved / all default cards deleted
 *   - Default card is expired
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.DTOs;
using UserAddressEntity = StyleNest.Infrastructure.Entities.Auth.UserAddress;

namespace StyleNest.Order.API.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Display-safe view of a saved shipping address.</summary>
public sealed record SavedAddressDto(
    Guid    Id,
    string  Label,
    string  RecipientName,
    string  AddressLine1,
    string? AddressLine2,
    string  City,
    string  State,
    string  PinCode);

/// <summary>
/// Express checkout eligibility snapshot returned by <c>GET /api/v1/checkout/express</c>.
/// When <see cref="CanExpressCheckout"/> is <see langword="false"/>, <see cref="BlockReason"/>
/// explains which precondition is unmet so the UI can guide the user.
/// </summary>
public sealed record ExpressCheckoutPreviewDto(
    bool             CanExpressCheckout,
    string?          BlockReason,
    SavedAddressDto? DefaultAddress,
    CardTokenDto?    DefaultCard);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface IExpressCheckoutService
{
    /// <summary>
    /// Returns the pre-filled address + card data and whether the user can proceed
    /// with express checkout without filling in any forms.
    /// </summary>
    Task<ExpressCheckoutPreviewDto> GetPreviewAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Places an order using the user's default saved address and default saved card.
    /// <para>
    /// Throws <see cref="InvalidOperationException"/> when:
    /// <list type="bullet">
    ///   <item>no default shipping address is saved;</item>
    ///   <item>no default card is saved (or the default was deleted);</item>
    ///   <item>the default card has expired.</item>
    /// </list>
    /// All other order-placement errors propagate from the underlying
    /// <see cref="IOrderService"/> (e.g. <see cref="Exceptions.InventoryValidationException"/>).
    /// </para>
    /// </summary>
    Task<OrderDto> PlaceExpressOrderAsync(
        Guid userId, string? couponCode = null, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class ExpressCheckoutService(
    AppDbContext  db,
    IOrderService orderService) : IExpressCheckoutService
{
    // ── GetPreviewAsync ───────────────────────────────────────────────────────

    public async Task<ExpressCheckoutPreviewDto> GetPreviewAsync(
        Guid userId, CancellationToken ct = default)
    {
        var address = await db.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.UserId == userId && a.IsDefault && !a.IsDeleted, ct);

        var card = await db.CardTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.IsDefault && !t.IsDeleted, ct);

        var now = DateTime.UtcNow;

        // Block reason checks in priority order (most critical first)
        if (address is null)
            return new ExpressCheckoutPreviewDto(
                CanExpressCheckout: false,
                BlockReason: "No default shipping address saved. "
                           + "Add one in your account settings to enable express checkout.",
                DefaultAddress: null,
                DefaultCard: null);

        if (card is null)
            return new ExpressCheckoutPreviewDto(
                CanExpressCheckout: false,
                BlockReason: "No default payment card saved. "
                           + "Save a card to your account to enable express checkout.",
                DefaultAddress: MapAddress(address),
                DefaultCard: null);

        if (IsExpiredCard(card, now))
            return new ExpressCheckoutPreviewDto(
                CanExpressCheckout: false,
                BlockReason: $"Your saved card ending in {card.Last4} has expired. "
                           + "Please update your payment method.",
                DefaultAddress: MapAddress(address),
                DefaultCard: MapCard(card, now));

        return new ExpressCheckoutPreviewDto(
            CanExpressCheckout: true,
            BlockReason: null,
            DefaultAddress: MapAddress(address),
            DefaultCard: MapCard(card, now));
    }

    // ── PlaceExpressOrderAsync ────────────────────────────────────────────────

    public async Task<OrderDto> PlaceExpressOrderAsync(
        Guid userId, string? couponCode = null, CancellationToken ct = default)
    {
        // ── Pre-flight validation ─────────────────────────────────────────────

        var address = await db.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.UserId == userId && a.IsDefault && !a.IsDeleted, ct)
            ?? throw new InvalidOperationException(
                "Express Checkout requires a default shipping address. "
              + "Please add one in your account settings.");

        var card = await db.CardTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.UserId == userId && t.IsDefault && !t.IsDeleted, ct)
            ?? throw new InvalidOperationException(
                "Express Checkout requires a saved default payment card. "
              + "Please save a card in your account settings.");

        if (IsExpiredCard(card, DateTime.UtcNow))
            throw new InvalidOperationException(
                $"Your saved card ending in {card.Last4} has expired. "
              + "Please update your payment method to proceed with express checkout.");

        // ── Build PlaceOrderRequest from saved defaults ────────────────────────

        var request = new PlaceOrderRequest(
            AddressLine1  : address.AddressLine1,
            AddressLine2  : address.AddressLine2,
            City          : address.City,
            State         : address.State,
            Pincode       : address.PinCode,
            PaymentMethod : "SAVED_CARD",
            CouponCode    : couponCode);

        return await orderService.PlaceOrderAsync(userId, request, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsExpiredCard(CardToken card, DateTime now) =>
        card.ExpiryYear < now.Year ||
        (card.ExpiryYear == now.Year && card.ExpiryMonth < now.Month);

    private static SavedAddressDto MapAddress(UserAddressEntity a) =>
        new(a.Id, a.Label, a.RecipientName, a.AddressLine1, a.AddressLine2,
            a.City, a.State, a.PinCode);

    private static CardTokenDto MapCard(CardToken t, DateTime now) =>
        new(t.Id,
            t.RazorpayTokenId,
            t.RazorpayCustomerId,
            t.Last4,
            t.Network,
            NetworkDisplay(t.Network),
            t.ExpiryMonth,
            t.ExpiryYear,
            t.CardholderName,
            t.IsDefault,
            IsExpired: IsExpiredCard(t, now),
            t.CreatedAt);

    private static string NetworkDisplay(CardNetwork network) => network switch
    {
        CardNetwork.Visa       => "Visa",
        CardNetwork.Mastercard => "Mastercard",
        CardNetwork.Amex       => "American Express",
        CardNetwork.Rupay      => "RuPay",
        CardNetwork.Maestro    => "Maestro",
        CardNetwork.Diners     => "Diners Club",
        _                      => "Unknown",
    };
}
