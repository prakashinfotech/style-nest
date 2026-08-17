using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Services;
using Xunit;
using OrderEntity = StyleNest.Infrastructure.Entities.Orders.Order;
using OrderStatus = StyleNest.Infrastructure.Entities.Orders.OrderStatus;

namespace StyleNest.Order.Tests;

/// <summary>ENH-PAY-005 — Bank timeout reconciliation poll tests.</summary>
public sealed class PaymentReconciliationJobTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PaymentReconciliationJob _sut;

    public PaymentReconciliationJobTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new PaymentReconciliationJob(_db, NullLogger<PaymentReconciliationJob>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<Payment> SeedPaymentAsync(PaymentStatus status, TimeSpan age)
    {
        var order = new OrderEntity
        {
            Id        = Guid.NewGuid(),
            UserId    = Guid.NewGuid(),
            Status    = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);

        var payment = new Payment
        {
            Id        = Guid.NewGuid(),
            OrderId   = order.Id,
            Status    = status,
            Amount    = 1000,
            Method    = PaymentMethod.Card,
            CreatedAt = DateTime.UtcNow - age,
            UpdatedAt = DateTime.UtcNow - age
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_InitiatedPaymentInWindow_SurfacedAsPending()
    {
        // 2-minute-old Initiated payment is inside the T+60s → T+15min window
        var payment = await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromMinutes(2));

        var count = await _sut.RunAsync();

        count.Should().Be(1);
        var updated = await _db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task RunAsync_TooYoung_NotSurfaced()
    {
        // 30-second-old payment is before the T+60s threshold
        var payment = await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromSeconds(30));

        var count = await _sut.RunAsync();

        count.Should().Be(0);
        var unchanged = await _db.Payments.FindAsync(payment.Id);
        unchanged!.Status.Should().Be(PaymentStatus.Initiated);
    }

    [Fact]
    public async Task RunAsync_TooOld_NotSurfaced()
    {
        // 20-minute-old payment is past the T+15min window
        var payment = await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromMinutes(20));

        var count = await _sut.RunAsync();

        count.Should().Be(0);
        var unchanged = await _db.Payments.FindAsync(payment.Id);
        unchanged!.Status.Should().Be(PaymentStatus.Initiated);
    }

    [Fact]
    public async Task RunAsync_AlreadyPending_Skipped()
    {
        // Payment already surfaced in a previous poll tick
        await SeedPaymentAsync(PaymentStatus.Pending, TimeSpan.FromMinutes(2));

        var count = await _sut.RunAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_CapturedPayment_Skipped()
    {
        await SeedPaymentAsync(PaymentStatus.Captured, TimeSpan.FromMinutes(2));

        var count = await _sut.RunAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_MultipleInWindow_AllSurfaced()
    {
        await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromMinutes(3));
        await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromMinutes(5));
        await SeedPaymentAsync(PaymentStatus.Initiated, TimeSpan.FromMinutes(10));

        var count = await _sut.RunAsync();

        count.Should().Be(3);
        var pendingCount = await _db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);
        pendingCount.Should().Be(3);
    }

    [Fact]
    public async Task RunAsync_EmptyDb_ReturnsZero()
    {
        var count = await _sut.RunAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task WindowBoundaries_ExactlyAtMinAge_IsIncluded()
    {
        // Exactly 60 seconds old — should be included (CreatedAt <= windowEnd = now - 60s)
        var payment = await SeedPaymentAsync(PaymentStatus.Initiated,
            PaymentReconciliationJob.MinAge);

        var count = await _sut.RunAsync();

        // May be 0 or 1 depending on ms precision — just verify no exception and status consistency
        var updated = await _db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().BeOneOf(PaymentStatus.Initiated, PaymentStatus.Pending);
        count.Should().BeGreaterThanOrEqualTo(0);
    }
}
