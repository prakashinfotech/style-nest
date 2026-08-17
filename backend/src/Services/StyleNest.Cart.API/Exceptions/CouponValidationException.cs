namespace StyleNest.Cart.API.Exceptions;

/// <summary>
/// Thrown when coupon validation fails with a specific, user-visible error code.
/// ErrorCode maps to one of 7 FR-CART-006 failure modes.
/// </summary>
public sealed class CouponValidationException(string errorCode, string message)
    : InvalidOperationException(message)
{
    public string ErrorCode { get; } = errorCode;
}
