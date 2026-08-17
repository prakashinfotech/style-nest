using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;
using StyleNest.User.API.Services;
using Xunit;

namespace StyleNest.User.Tests;

/// <summary>ENH-NOTIF-001 — Exponential backoff retry (1m, 3m, 9m → DLQ) tests.</summary>
public sealed class NotificationRetryJobTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<INotificationSender> _senderMock;
    private readonly Mock<INotificationDlqSink> _dlqMock;
    private readonly NotificationRetryJob _sut;

    public NotificationRetryJobTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db         = new AppDbContext(opts);
        _senderMock = new Mock<INotificationSender>();
        _dlqMock    = new Mock<INotificationDlqSink>();
        _sut        = new NotificationRetryJob(
            _db, _senderMock.Object, _dlqMock.Object,
            NullLogger<NotificationRetryJob>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<NotificationOutbox> SeedAsync(
        OutboxStatus status  = OutboxStatus.Pending,
        int attemptCount     = 0,
        DateTime? nextAttempt = null)
    {
        var item = new NotificationOutbox
        {
            Id            = Guid.NewGuid(),
            UserId        = Guid.NewGuid(),
            Type          = "OrderConfirmed",
            Subject       = "Your order is confirmed",
            Body          = "{}",
            Status        = status,
            AttemptCount  = attemptCount,
            NextAttemptAt = nextAttempt,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
        _db.NotificationOutbox.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendSucceeds_MarksDelivered()
    {
        var item = await SeedAsync();
        _senderMock.Setup(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default))
                   .ReturnsAsync(true);

        var count = await _sut.RunAsync();

        count.Should().Be(1);
        var updated = await _db.NotificationOutbox.FindAsync(item.Id);
        updated!.Status.Should().Be(OutboxStatus.Delivered);
        _dlqMock.Verify(d => d.EnqueueAsync(It.IsAny<NotificationOutbox>(), default), Times.Never);
    }

    [Fact]
    public async Task FirstFailure_SchedulesRetryAt1Min()
    {
        var item = await SeedAsync(attemptCount: 0);
        _senderMock.Setup(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default))
                   .ReturnsAsync(false);

        await _sut.RunAsync();

        var updated = await _db.NotificationOutbox.FindAsync(item.Id);
        updated!.Status.Should().Be(OutboxStatus.Pending);
        updated.AttemptCount.Should().Be(1);
        updated.NextAttemptAt.Should().BeCloseTo(
            DateTime.UtcNow.AddSeconds(NotificationRetryJob.RetryDelaysSeconds[0]),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SecondFailure_SchedulesRetryAt3Min()
    {
        // Simulate: first retry already fired (attemptCount=1), NextAttemptAt in the past
        var item = await SeedAsync(attemptCount: 1, nextAttempt: DateTime.UtcNow.AddSeconds(-1));
        _senderMock.Setup(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default))
                   .ReturnsAsync(false);

        await _sut.RunAsync();

        var updated = await _db.NotificationOutbox.FindAsync(item.Id);
        updated!.AttemptCount.Should().Be(2);
        updated.NextAttemptAt.Should().BeCloseTo(
            DateTime.UtcNow.AddSeconds(NotificationRetryJob.RetryDelaysSeconds[1]),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ThirdFailure_SchedulesRetryAt9Min()
    {
        var item = await SeedAsync(attemptCount: 2, nextAttempt: DateTime.UtcNow.AddSeconds(-1));
        _senderMock.Setup(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default))
                   .ReturnsAsync(false);

        await _sut.RunAsync();

        var updated = await _db.NotificationOutbox.FindAsync(item.Id);
        updated!.AttemptCount.Should().Be(3);
        updated.NextAttemptAt.Should().BeCloseTo(
            DateTime.UtcNow.AddSeconds(NotificationRetryJob.RetryDelaysSeconds[2]),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FourthFailure_DeadLetters()
    {
        // attemptCount=3 means we're about to make the 4th attempt (= MaxAttempts)
        var item = await SeedAsync(attemptCount: 3, nextAttempt: DateTime.UtcNow.AddSeconds(-1));
        _senderMock.Setup(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default))
                   .ReturnsAsync(false);

        await _sut.RunAsync();

        var updated = await _db.NotificationOutbox.FindAsync(item.Id);
        updated!.Status.Should().Be(OutboxStatus.DeadLettered);
        _dlqMock.Verify(d => d.EnqueueAsync(It.Is<NotificationOutbox>(n => n.Id == item.Id), default),
            Times.Once);
    }

    [Fact]
    public async Task FutureNextAttempt_IsSkipped()
    {
        // NextAttemptAt is in the future — should not be picked up yet
        await SeedAsync(nextAttempt: DateTime.UtcNow.AddMinutes(5));

        var count = await _sut.RunAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task AlreadyDelivered_IsSkipped()
    {
        await SeedAsync(status: OutboxStatus.Delivered);

        var count = await _sut.RunAsync();

        count.Should().Be(0);
        _senderMock.Verify(s => s.SendAsync(It.IsAny<NotificationOutbox>(), default), Times.Never);
    }

    [Fact]
    public async Task DeadLettered_IsSkipped()
    {
        await SeedAsync(status: OutboxStatus.DeadLettered);

        var count = await _sut.RunAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async Task EmptyOutbox_ReturnsZero()
    {
        var count = await _sut.RunAsync();
        count.Should().Be(0);
    }

    [Fact]
    public void RetryDelaysAreCorrect()
    {
        NotificationRetryJob.RetryDelaysSeconds.Should().Equal(60, 180, 540);
        NotificationRetryJob.MaxAttempts.Should().Be(4);
    }
}
