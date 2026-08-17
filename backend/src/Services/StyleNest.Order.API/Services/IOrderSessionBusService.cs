using StyleNest.Infrastructure.Entities.Orders;

namespace StyleNest.Order.API.Services;

/// <summary>
/// ENH-ORD-003 — Azure Service Bus Session Affinity.
///
/// Publishes order-lifecycle events to a session-enabled Service Bus queue
/// using the <c>orderId</c> as the session ID, guaranteeing strict FIFO
/// delivery for all state transitions that belong to the same order.
///
/// Contract:
///   • If the Service Bus connection string is not configured the implementation
///     is a no-op — callers MUST NOT branch on its result.
///   • All exceptions are swallowed and logged so that a messaging outage never
///     fails the HTTP response (fire-and-forget reliability pattern).
/// </summary>
public interface IOrderSessionBusService
{
    /// <summary>
    /// Publishes an <see cref="OrderSessionMessage"/> for the given order.
    /// The session ID is set to <paramref name="orderId"/>.ToString() so that
    /// all messages for the same order are processed in the order they were sent.
    /// </summary>
    Task PublishAsync(
        Guid        orderId,
        string      orderNumber,
        OrderStatus newStatus,
        string?     note            = null,
        CancellationToken ct        = default);
}

/// <summary>Wire-format for messages published to the order-events queue.</summary>
public sealed record OrderSessionMessage(
    Guid        OrderId,
    string      OrderNumber,
    string      NewStatus,
    string?     Note,
    DateTime    OccurredAtUtc);
