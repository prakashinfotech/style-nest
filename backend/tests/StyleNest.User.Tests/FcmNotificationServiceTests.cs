/**
 * ENH-NOTIF-002 — FCM Push Notifications
 * Acceptance criteria:
 *   - Order status change → FCM push with correct orderId and newStatus in payload
 *   - FCM device token stored per user/device; token refresh handled (old token replaced)
 *   - Stale token (FCM 404 UNREGISTERED) → token soft-deleted, not retried in same batch
 *   - Delivery receipt stored in NotificationLogs
 *   - No HTTP call when user has no registered tokens
 */

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StyleNest.Infrastructure.Entities.Notifications;
using StyleNest.Infrastructure.Persistence;
using StyleNest.User.API.Services;
using Xunit;

namespace StyleNest.User.Tests;

public sealed class FcmNotificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IHttpClientFactory> _factoryMock;
    private readonly FcmSettings _settings;

    public FcmNotificationServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db          = new AppDbContext(opts);
        _factoryMock = new Mock<IHttpClientFactory>();
        _settings    = new FcmSettings { ProjectId = "test-project", BearerToken = "test-token" };
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private FcmNotificationService BuildSut(HttpResponseMessage? fcmResponse = null)
    {
        fcmResponse ??= new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"projects/test-project/messages/1\"}"),
        };

        var handler = new FakeHttpMessageHandler(_ => fcmResponse);
        var client  = new HttpClient(handler);

        _factoryMock.Setup(f => f.CreateClient("fcm")).Returns(client);

        return new FcmNotificationService(
            _db,
            _factoryMock.Object,
            Options.Create(_settings),
            NullLogger<FcmNotificationService>.Instance);
    }

    private async Task<FcmDeviceToken> SeedTokenAsync(
        Guid userId, string deviceId = "dev-001", string token = "token-abc",
        bool isDeleted = false)
    {
        var entity = new FcmDeviceToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            DeviceId  = deviceId,
            Token     = token,
            Platform  = "web",
            IsDeleted = isDeleted,
        };
        _db.FcmDeviceTokens.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    // ── RegisterTokenAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterToken_NewDevice_CreatesToken()
    {
        var sut    = BuildSut();
        var userId = Guid.NewGuid();

        await sut.RegisterTokenAsync(userId, "dev-001", "my-fcm-token", "android");

        var stored = await _db.FcmDeviceTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == "dev-001");

        stored.Should().NotBeNull();
        stored!.Token.Should().Be("my-fcm-token");
        stored.Platform.Should().Be("android");
        stored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterToken_ExistingDevice_UpdatesToken()
    {
        var userId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-001", "old-token");
        var sut = BuildSut();

        await sut.RegisterTokenAsync(userId, "dev-001", "new-token", "web");

        var tokens = await _db.FcmDeviceTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.DeviceId == "dev-001")
            .ToListAsync();

        tokens.Should().HaveCount(1);
        tokens[0].Token.Should().Be("new-token");
    }

    [Fact]
    public async Task RegisterToken_SoftDeletedDevice_RestoresAndUpdatesToken()
    {
        var userId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-001", "stale-token", isDeleted: true);
        var sut = BuildSut();

        await sut.RegisterTokenAsync(userId, "dev-001", "fresh-token", "ios");

        var record = await _db.FcmDeviceTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == "dev-001");

        record!.Token.Should().Be("fresh-token");
        record.IsDeleted.Should().BeFalse();
    }

    // ── SendOrderUpdateAsync — no tokens ─────────────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_NoTokens_DoesNotCallHttpClient()
    {
        var sut = BuildSut();

        await sut.SendOrderUpdateAsync(Guid.NewGuid(), Guid.NewGuid(), "TC-001", "Shipped");

        _factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    // ── SendOrderUpdateAsync — successful delivery ────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_FcmAccepts_LogsDeliveredNotification()
    {
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-001", "tok-ok");

        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"projects/test-project/messages/123\"}"),
        });

        await sut.SendOrderUpdateAsync(userId, orderId, "TC-888", "Shipped");

        var log = await _db.NotificationLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.UserId == userId);

        log.Should().NotBeNull();
        log!.Type.Should().Be("FCM_ORDER_UPDATE");
        log.Message.Should().Contain(orderId.ToString());
        log.Message.Should().Contain("Shipped");
    }

    // ── SendOrderUpdateAsync — stale token ────────────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_StaleToken_SoftDeletesToken()
    {
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var token   = await SeedTokenAsync(userId, "dev-stale", "stale-token");

        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        await sut.SendOrderUpdateAsync(userId, orderId, "TC-999", "Shipped");

        // Reload bypassing soft-delete filter
        _db.Entry(token).Reload();
        token.IsDeleted.Should().BeTrue("stale token must be soft-deleted after 404 from FCM");
    }

    [Fact]
    public async Task SendOrderUpdate_StaleToken_LogsFailedNotification()
    {
        var userId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-stale", "stale-token");

        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        await sut.SendOrderUpdateAsync(userId, Guid.NewGuid(), "TC-999", "Shipped");

        var log = await _db.NotificationLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.UserId == userId);

        log!.Type.Should().Be("FCM_ORDER_UPDATE_FAILED");
    }

    // ── SendOrderUpdateAsync — FCM server error ───────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_FcmServerError_LogsFailedNotification()
    {
        var userId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-001", "tok");

        var sut = BuildSut(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"SERVICE_UNAVAILABLE\"}"),
        });

        await sut.SendOrderUpdateAsync(userId, Guid.NewGuid(), "TC-100", "Processing");

        var log = await _db.NotificationLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.UserId == userId);

        log!.Type.Should().Be("FCM_ORDER_UPDATE_FAILED");
    }

    // ── SendOrderUpdateAsync — correct FCM payload ────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_Payload_ContainsOrderIdAndNewStatus()
    {
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-001", "tok-payload");

        // Capture the request body INSIDE the handler before HttpClient disposes it
        string? capturedBody = null;
        var handler = new FakeHttpAsyncMessageHandler(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"projects/test-project/messages/1\"}"),
            };
        });
        var client = new HttpClient(handler);
        _factoryMock.Setup(f => f.CreateClient("fcm")).Returns(client);

        var sut = new FcmNotificationService(
            _db, _factoryMock.Object,
            Options.Create(_settings),
            NullLogger<FcmNotificationService>.Instance);

        await sut.SendOrderUpdateAsync(userId, orderId, "TC-7777", "Delivered");

        capturedBody.Should().NotBeNull();
        var doc  = JsonDocument.Parse(capturedBody!);
        var data = doc.RootElement.GetProperty("message").GetProperty("data");

        data.GetProperty("orderId").GetString().Should().Be(orderId.ToString());
        data.GetProperty("newStatus").GetString().Should().Be("Delivered");
        data.GetProperty("orderNumber").GetString().Should().Be("TC-7777");
    }

    // ── SendOrderUpdateAsync — multiple tokens ────────────────────────────────

    [Fact]
    public async Task SendOrderUpdate_MultipleTokens_SendsToEachAndLogsAll()
    {
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await SeedTokenAsync(userId, "dev-A", "tokA");
        await SeedTokenAsync(userId, "dev-B", "tokB");

        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"projects/test-project/messages/1\"}"),
            };
        });
        var client = new HttpClient(handler);
        _factoryMock.Setup(f => f.CreateClient("fcm")).Returns(client);

        var sut = new FcmNotificationService(
            _db, _factoryMock.Object,
            Options.Create(_settings),
            NullLogger<FcmNotificationService>.Instance);

        await sut.SendOrderUpdateAsync(userId, orderId, "TC-200", "Shipped");

        callCount.Should().Be(2, "one HTTP call per registered token");

        var logs = await _db.NotificationLogs
            .IgnoreQueryFilters()
            .Where(n => n.UserId == userId)
            .ToListAsync();

        logs.Should().HaveCount(2, "one NotificationLog entry per token");
        logs.Should().AllSatisfy(l => l.Type.Should().Be("FCM_ORDER_UPDATE"));
    }
}

/// <summary>Synchronous fake HttpMessageHandler for FCM mock responses.</summary>
internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(handler(request));
}

/// <summary>Async fake HttpMessageHandler — allows reading request body before disposal.</summary>
internal sealed class FakeHttpAsyncMessageHandler(
    Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => handler(request);
}
