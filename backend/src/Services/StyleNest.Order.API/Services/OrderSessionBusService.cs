using System.Text.Json;
using Azure.Messaging.ServiceBus;
using StyleNest.Infrastructure.Entities.Orders;

namespace StyleNest.Order.API.Services;

/// <summary>
/// ENH-ORD-003 — Azure Service Bus Session Affinity implementation.
///
/// Uses <see cref="ServiceBusSender"/> configured for a session-enabled queue.
/// Session ID = orderId.ToString() — the broker guarantees that messages with
/// the same session ID are delivered to the same consumer in strict FIFO order,
/// preventing out-of-order state transitions (e.g. Shipped arriving before Packed).
///
/// Configuration (appsettings.json / environment variables):
/// <code>
/// "ServiceBus": {
///   "ConnectionString": "Endpoint=sb://...",
///   "OrderEventsQueueName": "order-events"   // must have "Requires Session" = true
/// }
/// </code>
///
/// When <c>ConnectionString</c> is absent or empty, every call is a no-op.
/// </summary>
public sealed class OrderSessionBusService : IOrderSessionBusService, IAsyncDisposable
{
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusSender? _sender;
    private readonly ILogger<OrderSessionBusService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public OrderSessionBusService(
        IConfiguration config,
        ILogger<OrderSessionBusService> logger)
    {
        _logger = logger;

        var connStr   = config["ServiceBus:ConnectionString"];
        var queueName = config["ServiceBus:OrderEventsQueueName"] ?? "order-events";

        if (string.IsNullOrWhiteSpace(connStr))
        {
            _logger.LogInformation(
                "ENH-ORD-003: ServiceBus:ConnectionString not configured — order-event publishing disabled.");
            return;
        }

        _client = new ServiceBusClient(connStr);
        _sender = _client.CreateSender(queueName);

        _logger.LogInformation(
            "ENH-ORD-003: Azure Service Bus order-events queue '{Queue}' sender initialised.",
            queueName);
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        Guid        orderId,
        string      orderNumber,
        OrderStatus newStatus,
        string?     note       = null,
        CancellationToken ct   = default)
    {
        if (_sender is null) return; // no-op when not configured

        var payload = new OrderSessionMessage(
            OrderId:       orderId,
            OrderNumber:   orderNumber,
            NewStatus:     newStatus.ToString(),
            Note:          note,
            OccurredAtUtc: DateTime.UtcNow);

        var body    = BinaryData.FromString(JsonSerializer.Serialize(payload, JsonOpts));
        var message = new ServiceBusMessage(body)
        {
            // ── FIFO per order ────────────────────────────────────────────────
            SessionId      = orderId.ToString(),

            // ── Deduplication window: 10 minutes ─────────────────────────────
            // MessageId format prevents double-processing on retry
            MessageId      = $"{orderId}:{newStatus}:{payload.OccurredAtUtc:yyyyMMddHHmmss}",

            ContentType    = "application/json",
            Subject        = $"OrderStatus.{newStatus}",
        };

        try
        {
            await _sender.SendMessageAsync(message, ct);

            _logger.LogInformation(
                "ENH-ORD-003: Published OrderStatus.{Status} for Order {OrderNumber} (session={SessionId}).",
                newStatus, orderNumber, message.SessionId);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: log but never fail the HTTP response
            _logger.LogError(ex,
                "ENH-ORD-003: Failed to publish OrderStatus.{Status} for Order {OrderNumber} — Service Bus unavailable.",
                newStatus, orderNumber);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender is not null) await _sender.DisposeAsync();
        if (_client is not null) await _client.DisposeAsync();
    }
}
