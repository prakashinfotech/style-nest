namespace StyleNest.Order.API.Exceptions;

/// <summary>
/// Thrown when EF Core optimistic concurrency detects that a ProductVariant
/// was updated by another transaction between our inventory read and commit.
/// Implements EC-INV-001 (ENH-CART-004).
/// </summary>
public sealed class ConcurrentCheckoutException()
    : InvalidOperationException(
        "One or more items were purchased by someone else while you were checking out. " +
        "Please refresh your cart and try again.")
{
}
