using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.DTOs;
using StyleNest.Order.API.Services;
using StyleNest.SharedKernel.Exceptions;
using Xunit;
using CartEntity     = StyleNest.Infrastructure.Entities.Commerce.Cart;
using CartItemEntity = StyleNest.Infrastructure.Entities.Commerce.CartItem;
using OrderEntity    = StyleNest.Infrastructure.Entities.Orders.Order;
using OrderItemEntity = StyleNest.Infrastructure.Entities.Orders.OrderItem;
using OrderStatusEnum = StyleNest.Infrastructure.Entities.Orders.OrderStatus;
using OrderStatusHistoryEntity = StyleNest.Infrastructure.Entities.Orders.OrderStatusHistory;

namespace StyleNest.Order.Tests;

public sealed class OrderServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OrderService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(options);
        // Passthrough stub: email gate always allows in OrderService unit tests
        var checkoutAuth = new Mock<ICheckoutAuthorizationService>();
        checkoutAuth
            .Setup(s => s.ValidateEmailAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // Passthrough stub: cashback is tested separately in CashbackServiceTests
        var cashback = new Mock<ICashbackService>();
        cashback
            .Setup(c => c.CreditAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sut = new OrderService(_db, checkoutAuth.Object, cashback.Object, Mock.Of<IOrderSessionBusService>());
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedOrderAsync(OrderStatusEnum status)
    {
        var addressId = Guid.NewGuid();
        var address = new UserAddress
        {
            Id           = addressId,
            UserId       = _userId,
            Label        = "Home",
            RecipientName = "Test User",
            PhoneNumber  = "9999999999",
            AddressLine1 = "123 Main St",
            City         = "Mumbai",
            State        = "MH",
            PinCode      = "400001",
            IsDefault    = true
        };
        _db.UserAddresses.Add(address);

        var variantId = Guid.NewGuid();
        var order = new OrderEntity
        {
            Id                = Guid.NewGuid(),
            UserId            = _userId,
            OrderNumber       = $"ORD-TEST-{Guid.NewGuid():N}"[..20],
            Status            = status,
            SubTotal          = 500m,
            DiscountAmount    = 0m,
            DeliveryCharge    = 49m,
            TotalAmount       = 549m,
            ShippingAddressId = addressId,
            StatusHistory     = [new OrderStatusHistoryEntity { Status = status, Note = "Test" }],
            Items             = [new OrderItemEntity
            {
                Id               = Guid.NewGuid(),
                ProductVariantId = variantId,
                ProductName      = "Test Product",
                Quantity         = 1,
                UnitPrice        = 500m,
                TotalPrice       = 500m
            }]
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order.Id;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrderAsync_EmptyCart_ThrowsInvalidOperationException()
    {
        var request = new PlaceOrderRequest(
            "123 Main St", null, "Mumbai", "Maharashtra", "400001", "COD", null);

        var act = async () => await _sut.PlaceOrderAsync(_userId, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cart is empty*");
    }

    [Fact]
    public async Task GetOrdersAsync_MultipleOrders_ReturnsAllUserOrders()
    {
        await SeedOrderAsync(OrderStatusEnum.Pending);
        await SeedOrderAsync(OrderStatusEnum.Confirmed);

        var orders = await _sut.GetOrdersAsync(_userId);

        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderAsync_ValidId_ReturnsOrder()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Pending);

        var order = await _sut.GetOrderAsync(_userId, orderId);

        order.Should().NotBeNull();
        order!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetOrderAsync_OtherUserOrder_ReturnsNull()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Pending);
        var otherUserId = Guid.NewGuid();

        var order = await _sut.GetOrderAsync(otherUserId, orderId);

        order.Should().BeNull();
    }

    [Fact]
    public async Task CancelOrderAsync_PendingOrder_SetsStatusToCancelled()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Pending);

        await _sut.CancelOrderAsync(_userId, orderId);

        var order = await _db.Orders.FindAsync(orderId);
        order!.Status.Should().Be(OrderStatusEnum.Cancelled);
    }

    [Fact]
    public async Task CancelOrderAsync_ConfirmedOrder_SetsStatusToCancelled()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Confirmed);

        await _sut.CancelOrderAsync(_userId, orderId);

        var order = await _db.Orders.FindAsync(orderId);
        order!.Status.Should().Be(OrderStatusEnum.Cancelled);
    }

    [Fact]
    public async Task CancelOrderAsync_DeliveredOrder_ThrowsInvalidOperationException()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Delivered);

        var act = async () => await _sut.CancelOrderAsync(_userId, orderId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition order from*");
    }

    [Fact]
    public async Task CancelOrderAsync_ShippedOrder_ThrowsInvalidOperationException()
    {
        var orderId = await SeedOrderAsync(OrderStatusEnum.Shipped);

        var act = async () => await _sut.CancelOrderAsync(_userId, orderId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition order from*");
    }

    [Fact]
    public async Task CancelOrderAsync_NonExistentOrder_ThrowsKeyNotFoundException()
    {
        var act = async () => await _sut.CancelOrderAsync(_userId, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Order not found*");
    }

    [Theory]
    [InlineData(OrderStatusEnum.OutForDelivery)]
    [InlineData(OrderStatusEnum.Delivered)]
    [InlineData(OrderStatusEnum.Cancelled)]
    [InlineData(OrderStatusEnum.Returned)]
    public async Task CancelOrderAsync_TerminalOrLateStatus_ThrowsInvalidOperationException(OrderStatusEnum status)
    {
        var orderId = await SeedOrderAsync(status);

        var act = async () => await _sut.CancelOrderAsync(_userId, orderId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition order from*");
    }
}

/// <summary>ENH-ORD-001 — pure unit tests for OrderStateMachine (no DB required).</summary>
public sealed class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatusEnum.Pending,        OrderStatusEnum.Confirmed)]
    [InlineData(OrderStatusEnum.Pending,        OrderStatusEnum.Cancelled)]
    [InlineData(OrderStatusEnum.Confirmed,      OrderStatusEnum.Processing)]
    [InlineData(OrderStatusEnum.Confirmed,      OrderStatusEnum.Cancelled)]
    [InlineData(OrderStatusEnum.Processing,     OrderStatusEnum.Shipped)]
    [InlineData(OrderStatusEnum.Processing,     OrderStatusEnum.Cancelled)]
    [InlineData(OrderStatusEnum.Shipped,        OrderStatusEnum.OutForDelivery)]
    [InlineData(OrderStatusEnum.OutForDelivery, OrderStatusEnum.Delivered)]
    [InlineData(OrderStatusEnum.OutForDelivery, OrderStatusEnum.Returned)]
    [InlineData(OrderStatusEnum.Delivered,      OrderStatusEnum.Returned)]
    public void CanTransition_ValidPairs_ReturnsTrue(OrderStatusEnum from, OrderStatusEnum to)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatusEnum.Delivered,      OrderStatusEnum.Pending)]
    [InlineData(OrderStatusEnum.Delivered,      OrderStatusEnum.Confirmed)]
    [InlineData(OrderStatusEnum.Cancelled,      OrderStatusEnum.Confirmed)]
    [InlineData(OrderStatusEnum.Returned,       OrderStatusEnum.Pending)]
    [InlineData(OrderStatusEnum.Shipped,        OrderStatusEnum.Pending)]
    [InlineData(OrderStatusEnum.OutForDelivery, OrderStatusEnum.Cancelled)]
    public void CanTransition_InvalidPairs_ReturnsFalse(OrderStatusEnum from, OrderStatusEnum to)
    {
        OrderStateMachine.CanTransition(from, to).Should().BeFalse();
    }

    [Theory]
    [InlineData(OrderStatusEnum.Delivered, OrderStatusEnum.Pending)]
    [InlineData(OrderStatusEnum.Cancelled, OrderStatusEnum.Confirmed)]
    [InlineData(OrderStatusEnum.Returned,  OrderStatusEnum.Pending)]
    public void ThrowIfInvalid_IllegalTransition_ThrowsInvalidOperationException(OrderStatusEnum from, OrderStatusEnum to)
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(from, to);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Cannot transition order from '{from}' to '{to}'*");
    }

    [Fact]
    public void ThrowIfInvalid_TerminalState_MessageSaysTerminal()
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(OrderStatusEnum.Cancelled, OrderStatusEnum.Pending);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*terminal state*");
    }

    [Theory]
    [InlineData(OrderStatusEnum.Pending,   OrderStatusEnum.Confirmed)]
    [InlineData(OrderStatusEnum.Confirmed, OrderStatusEnum.Processing)]
    public void ThrowIfInvalid_ValidTransition_DoesNotThrow(OrderStatusEnum from, OrderStatusEnum to)
    {
        var act = () => OrderStateMachine.ThrowIfInvalid(from, to);

        act.Should().NotThrow();
    }
}

/// <summary>ENH-ORD-002 — pure unit tests for OrderStateConflictException (no DB required).</summary>
public sealed class OrderStateConflictExceptionTests
{
    [Fact]
    public void Constructor_SetsOrderId()
    {
        var id = Guid.NewGuid();
        var ex = new OrderStateConflictException(id);

        ex.OrderId.Should().Be(id);
    }

    [Fact]
    public void ErrorCode_IsOrderStateConflict()
    {
        var ex = new OrderStateConflictException(Guid.NewGuid());

        ex.ErrorCode.Should().Be("ORDER_STATE_CONFLICT");
    }

    [Fact]
    public void Message_ContainsOrderId()
    {
        var id = Guid.NewGuid();
        var ex = new OrderStateConflictException(id);

        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void Exception_InheritsFromException_NotInvalidOperationException()
    {
        var ex = new OrderStateConflictException(Guid.NewGuid());

        ex.Should().BeAssignableTo<Exception>();
        ex.Should().NotBeAssignableTo<InvalidOperationException>();
    }
}
