namespace StyleNest.SharedKernel.Exceptions;

/// <summary>
/// Thrown when two concurrent requests attempt to transition an order's status at the same time.
/// The EF Core rowversion detects the lost-update and surfaces this as HTTP 409 with
/// errorCode ORDER_STATE_CONFLICT (ENH-ORD-002 / FR-ORD-002 / TC-ORD-FUNC-036).
/// </summary>
public sealed class OrderStateConflictException(Guid orderId)
    : Exception(
        $"Order '{orderId}' was modified by another request. " +
        "Refresh the order and retry the status update.")
{
    public Guid OrderId { get; } = orderId;
    public string ErrorCode => "ORDER_STATE_CONFLICT";
}
