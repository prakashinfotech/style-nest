namespace StyleNest.Order.API.Exceptions;

/// <summary>
/// Represents an item that is unavailable or has insufficient stock at checkout.
/// </summary>
public sealed record OosItem(
    Guid   VariantId,
    string ProductName,
    string VariantDetails,
    int    RequestedQuantity,
    int    AvailableQuantity
);

/// <summary>
/// Thrown during PlaceOrder / BuyNow when inventory re-validation detects stock shortfalls.
/// Implements EC-INV-002 (ENH-CART-003).
/// </summary>
public sealed class InventoryValidationException(IReadOnlyList<OosItem> items)
    : InvalidOperationException("One or more items in your cart are unavailable in the requested quantity.")
{
    public IReadOnlyList<OosItem> OutOfStockItems { get; } = items;
}
