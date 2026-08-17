using System.Text.Json.Serialization;

namespace StyleNest.Order.API.DTOs;

public sealed record RazorpayWebhookPayload(
    [property: JsonPropertyName("event")]   string Event,
    [property: JsonPropertyName("payload")] RazorpayWebhookInner Payload);

public sealed record RazorpayWebhookInner(
    [property: JsonPropertyName("payment")] RazorpayPaymentWrapper Payment);

public sealed record RazorpayPaymentWrapper(
    [property: JsonPropertyName("entity")] RazorpayPaymentEntity Entity);

public sealed record RazorpayPaymentEntity(
    [property: JsonPropertyName("id")]       string Id,
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("amount")]   long   Amount,
    [property: JsonPropertyName("status")]   string Status);
