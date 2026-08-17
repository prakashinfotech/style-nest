/**
 * ENH-PAY-006 — Razorpay Vault Tokenisation
 * PCI-DSS compliance: no PAN, no CVV, no full card number is ever persisted.
 * Only Razorpay's opaque vault token_id + display-safe metadata (last-4, network,
 * expiry, cardholder name) are stored. The real card data lives exclusively in
 * Razorpay's PCI-certified vault.
 */

using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Payments;

/// <summary>Card payment networks supported by Razorpay vault tokenisation.</summary>
public enum CardNetwork
{
    Unknown    = 0,
    Visa       = 1,
    Mastercard = 2,
    Amex       = 3,
    Rupay      = 4,
    Maestro    = 5,
    Diners     = 6,
}

/// <summary>
/// ENH-PAY-006 — A Razorpay vault card token. Stores the opaque
/// <see cref="RazorpayTokenId"/> returned by the Razorpay Cards API after the
/// customer authorises the first transaction, plus display-safe metadata.
/// <para>
/// <b>Security invariant:</b> This entity MUST NEVER store PAN, CVV, full expiry
/// in any encoded form, or any data classified as CHD (Cardholder Data) by PCI-DSS.
/// </para>
/// </summary>
public class CardToken : BaseEntity<Guid>
{
    /// <summary>Owner of this saved card — always filtered by this in service queries.</summary>
    public Guid UserId { get; set; }

    // ── Razorpay vault reference ──────────────────────────────────────────────

    /// <summary>
    /// Opaque token ID from Razorpay vault (e.g. "token_XXXXXXXXXXXXXXXX").
    /// Used in subsequent charge requests instead of raw card data.
    /// </summary>
    public string RazorpayTokenId { get; set; } = string.Empty;

    /// <summary>
    /// Razorpay customer ID associated with this token, used for recurring charges.
    /// May be null for guest tokenisation flows.
    /// </summary>
    public string? RazorpayCustomerId { get; set; }

    // ── Display-safe card metadata (PCI-DSS non-CHD) ─────────────────────────

    /// <summary>Last 4 digits of the card — safe to store and display.</summary>
    public string Last4 { get; set; } = string.Empty;

    /// <summary>Card network (Visa, Mastercard, etc.) for display/routing purposes.</summary>
    public CardNetwork Network { get; set; } = CardNetwork.Unknown;

    /// <summary>Card expiry month (1–12). Does not constitute CHD under PCI-DSS.</summary>
    public int ExpiryMonth { get; set; }

    /// <summary>Card expiry year (4-digit). Does not constitute CHD under PCI-DSS.</summary>
    public int ExpiryYear { get; set; }

    /// <summary>Optional cardholder name as printed on the card (display only).</summary>
    public string? CardholderName { get; set; }

    // ── UX metadata ──────────────────────────────────────────────────────────

    /// <summary>
    /// True when this is the user's selected default card for checkout pre-fill.
    /// At most one token per user should have IsDefault=true (enforced by service).
    /// </summary>
    public bool IsDefault { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ApplicationUser User { get; set; } = null!;
}
