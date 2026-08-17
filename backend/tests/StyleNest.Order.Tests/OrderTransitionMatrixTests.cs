/**
 * ENH-ORD-004 — Full Invalid State Transition Matrix (FR-ORD-002 / TC-ORD-FUNC-041..045)
 * Acceptance criteria tested here:
 *
 *   TC-ORD-FUNC-041: Shipped → Cancelled is rejected (cannot cancel in-transit)
 *   TC-ORD-FUNC-042: Shipped → Confirmed is rejected (backward transition blocked)
 *   TC-ORD-FUNC-043: Cancelled → Pending is rejected (terminal state; no resurrection)
 *   TC-ORD-FUNC-044: Returned → Pending is rejected (terminal state; no resurrection)
 *   TC-ORD-FUNC-045: OutForDelivery → Cancelled is rejected (cannot cancel out-for-delivery)
 *
 *   Additionally:
 *   - All 10 valid transitions pass CanTransition = true
 *   - All other (from, to) pairs that are not valid transitions return CanTransition = false
 *   - ThrowIfInvalid for every blocked transition throws InvalidOperationException
 *     with a message naming both the from-state and the to-state
 *   - ThrowIfInvalid for terminal states includes "terminal state" in the message
 *   - ThrowIfInvalid for every valid transition does NOT throw
 */

using FluentAssertions;
using StyleNest.Infrastructure.Entities.Orders;
using Xunit;
using S = StyleNest.Infrastructure.Entities.Orders.OrderStatus;

namespace StyleNest.Order.Tests;

/// <summary>
/// ENH-ORD-004 — Comprehensive invalid state-transition matrix.
/// Each TC-ORD-FUNC-041..045 case is covered by a dedicated named fact,
/// followed by exhaustive theory tests for the complete matrix.
/// </summary>
public sealed class OrderTransitionMatrixTests
{
    // ── TC-ORD-FUNC-041..045 — explicit named acceptance-criteria tests ────────

    /// <summary>TC-ORD-FUNC-041 — Cannot cancel an order that has already shipped.</summary>
    [Fact]
    public void TcOrdFunc041_ShippedToCancelled_IsBlocked()
    {
        OrderStateMachine.CanTransition(S.Shipped, S.Cancelled)
            .Should().BeFalse(
                because: "TC-ORD-FUNC-041: once an order is shipped the carrier has collected " +
                         "the parcel — cancellation is no longer possible");

        var act = () => OrderStateMachine.ThrowIfInvalid(S.Shipped, S.Cancelled);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Shipped*")
            .And.Message.Should().Contain("Cancelled");
    }

    /// <summary>TC-ORD-FUNC-042 — Shipped cannot transition back to Confirmed (backward).</summary>
    [Fact]
    public void TcOrdFunc042_ShippedToConfirmed_IsBlocked()
    {
        OrderStateMachine.CanTransition(S.Shipped, S.Confirmed)
            .Should().BeFalse(
                because: "TC-ORD-FUNC-042: backward transitions are never permitted; " +
                         "Shipped → Confirmed would un-ship a parcel already in transit");

        var act = () => OrderStateMachine.ThrowIfInvalid(S.Shipped, S.Confirmed);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Shipped*")
            .And.Message.Should().Contain("Confirmed");
    }

    /// <summary>TC-ORD-FUNC-043 — Cancelled is a terminal state; no forward transition allowed.</summary>
    [Fact]
    public void TcOrdFunc043_CancelledToPending_IsBlocked()
    {
        OrderStateMachine.CanTransition(S.Cancelled, S.Pending)
            .Should().BeFalse(
                because: "TC-ORD-FUNC-043: Cancelled is a terminal state — " +
                         "no transition out of it is ever permitted");

        var act = () => OrderStateMachine.ThrowIfInvalid(S.Cancelled, S.Pending);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*terminal state*",
                because: "the error message must mention 'terminal state' so callers can distinguish " +
                         "terminal-state rejections from mid-flow invalid-transition rejections");
    }

    /// <summary>TC-ORD-FUNC-044 — Returned is a terminal state; no forward transition allowed.</summary>
    [Fact]
    public void TcOrdFunc044_ReturnedToPending_IsBlocked()
    {
        OrderStateMachine.CanTransition(S.Returned, S.Pending)
            .Should().BeFalse(
                because: "TC-ORD-FUNC-044: Returned is a terminal state — " +
                         "a returned order cannot be re-activated");

        var act = () => OrderStateMachine.ThrowIfInvalid(S.Returned, S.Pending);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*terminal state*");
    }

    /// <summary>TC-ORD-FUNC-045 — OutForDelivery orders cannot be cancelled mid-delivery run.</summary>
    [Fact]
    public void TcOrdFunc045_OutForDeliveryToCancelled_IsBlocked()
    {
        OrderStateMachine.CanTransition(S.OutForDelivery, S.Cancelled)
            .Should().BeFalse(
                because: "TC-ORD-FUNC-045: the parcel is on the delivery vehicle — " +
                         "cancellation must be routed through NDR / Return, not directly to Cancelled");

        var act = () => OrderStateMachine.ThrowIfInvalid(S.OutForDelivery, S.Cancelled);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OutForDelivery*")
            .And.Message.Should().Contain("Cancelled");
    }

    // ── Complete valid-transition matrix ──────────────────────────────────────

    [Theory]
    [InlineData(S.Pending,        S.Confirmed)]
    [InlineData(S.Pending,        S.Cancelled)]
    [InlineData(S.Confirmed,      S.Processing)]
    [InlineData(S.Confirmed,      S.Cancelled)]
    [InlineData(S.Processing,     S.Shipped)]
    [InlineData(S.Processing,     S.Cancelled)]
    [InlineData(S.Shipped,        S.OutForDelivery)]
    [InlineData(S.OutForDelivery, S.Delivered)]
    [InlineData(S.OutForDelivery, S.Returned)]
    [InlineData(S.Delivered,      S.Returned)]
    public void CanTransition_AllValidPairs_ReturnTrue(S from, S to)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeTrue(
            because: $"{from} → {to} is a legal forward transition in the order workflow");
    }

    [Theory]
    [InlineData(S.Pending,        S.Confirmed)]
    [InlineData(S.Pending,        S.Cancelled)]
    [InlineData(S.Confirmed,      S.Processing)]
    [InlineData(S.Confirmed,      S.Cancelled)]
    [InlineData(S.Processing,     S.Shipped)]
    [InlineData(S.Processing,     S.Cancelled)]
    [InlineData(S.Shipped,        S.OutForDelivery)]
    [InlineData(S.OutForDelivery, S.Delivered)]
    [InlineData(S.OutForDelivery, S.Returned)]
    [InlineData(S.Delivered,      S.Returned)]
    public void ThrowIfInvalid_AllValidPairs_DoesNotThrow(S from, S to)
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(from, to);
        act.Should().NotThrow(
            because: $"{from} → {to} is a legal transition and must never throw");
    }

    // ── Complete invalid-transition matrix ────────────────────────────────────

    /// <summary>
    /// Every (from, to) pair that is not in the valid-transition table must be rejected.
    /// Pairs are grouped by domain category for clarity:
    ///   • Backward transitions (reversal of workflow direction)
    ///   • State-skipping transitions (jumping over one or more steps)
    ///   • Terminal-state outgoing transitions (Cancelled/Returned → anything)
    ///   • Same-domain lateral moves (Shipped → Confirmed, OutForDelivery → Processing, etc.)
    /// </summary>
    [Theory]
    // ── Backward transitions ──────────────────────────────────────────────────
    [InlineData(S.Confirmed,      S.Pending,        "backward")]
    [InlineData(S.Processing,     S.Confirmed,      "backward")]
    [InlineData(S.Processing,     S.Pending,        "backward")]
    [InlineData(S.Shipped,        S.Processing,     "backward")]
    [InlineData(S.Shipped,        S.Confirmed,      "backward")]   // TC-ORD-FUNC-042
    [InlineData(S.Shipped,        S.Pending,        "backward")]
    [InlineData(S.OutForDelivery, S.Shipped,        "backward")]
    [InlineData(S.OutForDelivery, S.Processing,     "backward")]
    [InlineData(S.OutForDelivery, S.Confirmed,      "backward")]
    [InlineData(S.OutForDelivery, S.Pending,        "backward")]
    [InlineData(S.Delivered,      S.OutForDelivery, "backward")]
    [InlineData(S.Delivered,      S.Shipped,        "backward")]
    [InlineData(S.Delivered,      S.Processing,     "backward")]
    [InlineData(S.Delivered,      S.Confirmed,      "backward")]
    [InlineData(S.Delivered,      S.Pending,        "backward")]
    // ── Cancellation-blocked transitions ─────────────────────────────────────
    [InlineData(S.Shipped,        S.Cancelled,      "cancel-blocked")]  // TC-ORD-FUNC-041
    [InlineData(S.OutForDelivery, S.Cancelled,      "cancel-blocked")]  // TC-ORD-FUNC-045
    [InlineData(S.Delivered,      S.Cancelled,      "cancel-blocked")]
    [InlineData(S.Returned,       S.Cancelled,      "terminal")]
    // ── State-skipping transitions ────────────────────────────────────────────
    [InlineData(S.Pending,        S.Processing,     "skip")]
    [InlineData(S.Pending,        S.Shipped,        "skip")]
    [InlineData(S.Pending,        S.OutForDelivery, "skip")]
    [InlineData(S.Pending,        S.Delivered,      "skip")]
    [InlineData(S.Pending,        S.Returned,       "skip")]
    [InlineData(S.Confirmed,      S.Shipped,        "skip")]
    [InlineData(S.Confirmed,      S.OutForDelivery, "skip")]
    [InlineData(S.Confirmed,      S.Delivered,      "skip")]
    [InlineData(S.Confirmed,      S.Returned,       "skip")]
    [InlineData(S.Processing,     S.OutForDelivery, "skip")]
    [InlineData(S.Processing,     S.Delivered,      "skip")]
    [InlineData(S.Processing,     S.Returned,       "skip")]
    [InlineData(S.Shipped,        S.Delivered,      "skip")]
    [InlineData(S.Shipped,        S.Returned,       "skip")]
    // ── Terminal-state outgoing transitions ───────────────────────────────────
    [InlineData(S.Cancelled,      S.Pending,        "terminal")]  // TC-ORD-FUNC-043
    [InlineData(S.Cancelled,      S.Confirmed,      "terminal")]
    [InlineData(S.Cancelled,      S.Processing,     "terminal")]
    [InlineData(S.Cancelled,      S.Shipped,        "terminal")]
    [InlineData(S.Cancelled,      S.OutForDelivery, "terminal")]
    [InlineData(S.Cancelled,      S.Delivered,      "terminal")]
    [InlineData(S.Cancelled,      S.Returned,       "terminal")]
    [InlineData(S.Returned,       S.Pending,        "terminal")]  // TC-ORD-FUNC-044
    [InlineData(S.Returned,       S.Confirmed,      "terminal")]
    [InlineData(S.Returned,       S.Processing,     "terminal")]
    [InlineData(S.Returned,       S.Shipped,        "terminal")]
    [InlineData(S.Returned,       S.OutForDelivery, "terminal")]
    [InlineData(S.Returned,       S.Delivered,      "terminal")]
    public void CanTransition_FullInvalidMatrix_ReturnsFalse(S from, S to, string category)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeFalse(
            because: $"[{category}] {from} → {to} is not a permitted workflow transition");
    }

    [Theory]
    [InlineData(S.Shipped,        S.Cancelled)]   // TC-ORD-FUNC-041
    [InlineData(S.Shipped,        S.Confirmed)]   // TC-ORD-FUNC-042
    [InlineData(S.Cancelled,      S.Pending)]     // TC-ORD-FUNC-043
    [InlineData(S.Returned,       S.Pending)]     // TC-ORD-FUNC-044
    [InlineData(S.OutForDelivery, S.Cancelled)]   // TC-ORD-FUNC-045
    [InlineData(S.Delivered,      S.Pending)]
    [InlineData(S.Processing,     S.Confirmed)]
    [InlineData(S.Pending,        S.Delivered)]
    public void ThrowIfInvalid_BlockedTransition_ThrowsWithBothStateNamesInMessage(S from, S to)
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(from, to);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(from.ToString(),
                because: "the error message must name the FROM state so operators can debug misrouted updates")
            .And.Contain(to.ToString(),
                because: "the error message must name the TO state so operators can debug misrouted updates");
    }

    [Theory]
    [InlineData(S.Cancelled, S.Pending)]     // TC-ORD-FUNC-043
    [InlineData(S.Cancelled, S.Confirmed)]
    [InlineData(S.Cancelled, S.Returned)]
    [InlineData(S.Returned,  S.Pending)]     // TC-ORD-FUNC-044
    [InlineData(S.Returned,  S.Confirmed)]
    [InlineData(S.Returned,  S.Cancelled)]
    public void ThrowIfInvalid_TerminalStateTransition_MessageIndicatesTerminalState(S from, S to)
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(from, to);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*terminal state*",
                because: "terminal-state transitions must be clearly distinguished " +
                         "from mid-flow invalid-transition errors in the error message");
    }
}
