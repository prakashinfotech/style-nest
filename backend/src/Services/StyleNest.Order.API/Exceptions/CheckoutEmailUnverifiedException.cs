namespace StyleNest.Order.API.Exceptions;

/// <summary>
/// ENH-CHKOUT-001 — Thrown when a user with an unverified email
/// attempts to place an order above the ₹5,000 threshold (BR-AUTH-003).
/// Maps to HTTP 403 CHECKOUT_EMAIL_UNVERIFIED in OrdersController.
/// </summary>
public sealed class CheckoutEmailUnverifiedException(string email)
    : Exception($"Email verification required for orders above ₹5,000. Please verify {email}.")
{
    public string Email { get; } = email;
}
