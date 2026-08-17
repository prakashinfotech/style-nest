using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.DTOs;

namespace StyleNest.Order.API.Services;

public interface IPaymentWebhookService
{
    bool VerifySignature(string rawBody, string signature);
    Task ProcessAsync(string rawBody, CancellationToken ct = default);
}

public sealed class PaymentWebhookService(
    AppDbContext db,
    IConfiguration config,
    ILogger<PaymentWebhookService> logger) : IPaymentWebhookService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Verifies a Razorpay HMAC-SHA256 webhook signature using constant-time comparison
    /// to prevent timing side-channel attacks (ENH-PAY-002 / FR-PAY-009).
    /// </summary>
    public bool VerifySignature(string rawBody, string signature)
    {
        var secret = config["RazorpayWebhook:Secret"] ?? string.Empty;
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
            return false;

        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var computedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computedHex   = Convert.ToHexString(computedBytes).ToLowerInvariant(); // always 64 chars

        var signatureHex = signature.ToLowerInvariant();

        // SHA-256 HMAC is always 32 bytes = 64 hex chars; reject anything else without timing leak
        if (computedHex.Length != signatureHex.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex),
            Encoding.UTF8.GetBytes(signatureHex));
    }

    public async Task ProcessAsync(string rawBody, CancellationToken ct = default)
    {
        RazorpayWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<RazorpayWebhookPayload>(rawBody, JsonOpts);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Webhook payload deserialization failed");
            return;
        }

        if (payload is null) return;

        if (payload.Event == "payment.captured" && payload.Payload?.Payment?.Entity is { } entity)
            await HandlePaymentCapturedAsync(entity, ct);
        else
            logger.LogInformation("Webhook: unhandled event '{Event}'", payload.Event);
    }

    private async Task HandlePaymentCapturedAsync(RazorpayPaymentEntity entity, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.StatusHistory)
            .FirstOrDefaultAsync(p => p.GatewayOrderId == entity.OrderId, ct);

        if (payment is null)
        {
            logger.LogWarning(
                "Webhook: no Payment found for gateway order {GatewayOrderId}", entity.OrderId);
            return;
        }

        payment.Status           = PaymentStatus.Captured;
        payment.GatewayPaymentId = entity.Id;
        payment.PaidAt           = DateTime.UtcNow;

        if (OrderStateMachine.CanTransition(payment.Order.Status, OrderStatus.Confirmed))
        {
            payment.Order.Status = OrderStatus.Confirmed;
            payment.Order.StatusHistory.Add(new OrderStatusHistory
            {
                OrderId = payment.Order.Id,
                Status  = OrderStatus.Confirmed,
                Note    = "Payment captured via Razorpay webhook"
            });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Webhook: payment {GatewayPaymentId} captured for order {OrderId}",
            entity.Id, payment.OrderId);
    }
}
