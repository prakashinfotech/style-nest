namespace StyleNest.Infrastructure.Entities.Orders;

/// <summary>
/// Defines legal OrderStatus transitions (FR-ORD-002 / ENH-ORD-001).
/// Terminal states Cancelled and Returned have no outgoing transitions.
/// </summary>
public static class OrderStateMachine
{
    private static readonly IReadOnlyDictionary<OrderStatus, IReadOnlySet<OrderStatus>> ValidTransitions =
        new Dictionary<OrderStatus, IReadOnlySet<OrderStatus>>
        {
            [OrderStatus.Pending]        = new HashSet<OrderStatus> { OrderStatus.Confirmed,  OrderStatus.Cancelled },
            [OrderStatus.Confirmed]      = new HashSet<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing]     = new HashSet<OrderStatus> { OrderStatus.Shipped,    OrderStatus.Cancelled },
            [OrderStatus.Shipped]        = new HashSet<OrderStatus> { OrderStatus.OutForDelivery },
            [OrderStatus.OutForDelivery] = new HashSet<OrderStatus> { OrderStatus.Delivered,  OrderStatus.Returned  },
            [OrderStatus.Delivered]      = new HashSet<OrderStatus> { OrderStatus.Returned },
            [OrderStatus.Cancelled]      = new HashSet<OrderStatus>(),
            [OrderStatus.Returned]       = new HashSet<OrderStatus>(),
        };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Throws <see cref="InvalidOperationException"/> when the transition is illegal.</summary>
    public static void ThrowIfInvalid(OrderStatus from, OrderStatus to)
    {
        if (CanTransition(from, to)) return;

        var allowed = ValidTransitions.TryGetValue(from, out var set) && set.Count > 0
            ? string.Join(", ", set)
            : "none (terminal state)";

        throw new InvalidOperationException(
            $"Cannot transition order from '{from}' to '{to}'. " +
            $"Allowed transitions from '{from}': {allowed}.");
    }
}
