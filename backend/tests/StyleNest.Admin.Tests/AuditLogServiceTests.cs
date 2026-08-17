/**
 * ENH-ADMIN-001 — AuditLogs Schema (append-only, 7y financial / 3y non-financial)
 * Acceptance criteria:
 *   - LogAsync writes an AuditLog row to DB with correct fields
 *   - Financial entries: ExpiresAt ≈ Timestamp + 7 years, RetentionCategory = Financial
 *   - Non-financial entries: ExpiresAt ≈ Timestamp + 3 years, RetentionCategory = NonFinancial
 *   - LogAsync never throws (swallows exceptions, logs error)
 *   - GetPagedAsync returns all entries when no filter applied
 *   - GetPagedAsync filters by action
 *   - GetPagedAsync filters by entityType
 *   - GetPagedAsync filters by actorId
 *   - GetPagedAsync returns correct TotalCount and paged Items
 *   - GetPagedAsync orders by Timestamp descending
 *   - RetentionCategory.Financial = 1, NonFinancial = 0
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Admin.API.DTOs;
using StyleNest.Admin.API.Services;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Admin.Tests;

public sealed class AuditLogServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new AuditLogService(_db, NullLogger<AuditLogService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── RetentionCategory enum values ──────────────────────────────────────────

    [Fact]
    public void RetentionCategory_NonFinancial_IsZero()
        => ((int)RetentionCategory.NonFinancial).Should().Be(0);

    [Fact]
    public void RetentionCategory_Financial_IsOne()
        => ((int)RetentionCategory.Financial).Should().Be(1);

    // ── LogAsync — row written ─────────────────────────────────────────────────

    [Fact]
    public async Task LogAsync_WritesRowToDatabase()
    {
        var entry = new AuditLogEntry("Banner.Created", "Banner", "abc-123");

        await _sut.LogAsync(entry);

        var log = await _db.AuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Action.Should().Be("Banner.Created");
        log.EntityType.Should().Be("Banner");
        log.EntityId.Should().Be("abc-123");
    }

    [Fact]
    public async Task LogAsync_Financial_SetsSevenYearExpiry()
    {
        var before = DateTime.UtcNow;
        await _sut.LogAsync(new AuditLogEntry("Payment.Captured", "Payment", IsFinancial: true));

        var log = await _db.AuditLogs.FirstAsync();
        var expectedExpiry = log.Timestamp.AddYears(7);
        log.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        log.RetentionCategory.Should().Be(RetentionCategory.Financial);
    }

    [Fact]
    public async Task LogAsync_NonFinancial_SetsThreeYearExpiry()
    {
        await _sut.LogAsync(new AuditLogEntry("Banner.Created", "Banner", IsFinancial: false));

        var log = await _db.AuditLogs.FirstAsync();
        var expectedExpiry = log.Timestamp.AddYears(3);
        log.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        log.RetentionCategory.Should().Be(RetentionCategory.NonFinancial);
    }

    [Fact]
    public async Task LogAsync_StoresAllFields()
    {
        var actorId = Guid.NewGuid();
        var entry = new AuditLogEntry(
            Action:     "Order.StatusChanged",
            EntityType: "Order",
            EntityId:   "order-001",
            OldValue:   "Pending",
            NewValue:   "Shipped",
            ActorId:    actorId,
            ActorName:  "admin@test.com",
            IpAddress:  "192.168.1.1",
            IsFinancial: true);

        await _sut.LogAsync(entry);

        var log = await _db.AuditLogs.FirstAsync();
        log.ActorId.Should().Be(actorId);
        log.ActorName.Should().Be("admin@test.com");
        log.OldValue.Should().Be("Pending");
        log.NewValue.Should().Be("Shipped");
        log.IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task LogAsync_SystemActor_ActorIdIsNull()
    {
        await _sut.LogAsync(new AuditLogEntry("Cron.RetentionCleanup", "System"));

        var log = await _db.AuditLogs.FirstAsync();
        log.ActorId.Should().BeNull();
    }

    // ── GetPagedAsync — no filter ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoFilter_ReturnsAllEntries()
    {
        await SeedLogsAsync(3);

        var result = await _sut.GetPagedAsync(null, null, null, 1, 25);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByTimestampDescending()
    {
        await SeedLogsAsync(3);

        var result = await _sut.GetPagedAsync(null, null, null, 1, 25);

        result.Items.Should().BeInDescendingOrder(x => x.Timestamp);
    }

    // ── GetPagedAsync — filtering ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FilterByAction_ReturnsMatchingOnly()
    {
        await _sut.LogAsync(new AuditLogEntry("Banner.Created", "Banner"));
        await _sut.LogAsync(new AuditLogEntry("Coupon.Deleted", "Coupon"));

        var result = await _sut.GetPagedAsync("Banner.Created", null, null, 1, 25);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Action.Should().Be("Banner.Created");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByEntityType_ReturnsMatchingOnly()
    {
        await _sut.LogAsync(new AuditLogEntry("Banner.Created", "Banner"));
        await _sut.LogAsync(new AuditLogEntry("Order.Cancelled", "Order"));

        var result = await _sut.GetPagedAsync(null, "Order", null, 1, 25);

        result.TotalCount.Should().Be(1);
        result.Items.Single().EntityType.Should().Be("Order");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByActorId_ReturnsMatchingOnly()
    {
        var actor = Guid.NewGuid();
        await _sut.LogAsync(new AuditLogEntry("Banner.Created", "Banner", ActorId: actor));
        await _sut.LogAsync(new AuditLogEntry("Banner.Created", "Banner", ActorId: Guid.NewGuid()));

        var result = await _sut.GetPagedAsync(null, null, actor, 1, 25);

        result.TotalCount.Should().Be(1);
        result.Items.Single().ActorId.Should().Be(actor);
    }

    // ── GetPagedAsync — pagination ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_Pagination_ReturnsCorrectPage()
    {
        await SeedLogsAsync(5);

        var result = await _sut.GetPagedAsync(null, null, null, page: 2, pageSize: 2);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task SeedLogsAsync(int count)
    {
        for (var i = 0; i < count; i++)
            await _sut.LogAsync(new AuditLogEntry($"Action.{i}", "Test", $"id-{i}"));
    }
}
