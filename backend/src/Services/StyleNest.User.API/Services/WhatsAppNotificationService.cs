using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StyleNest.Infrastructure.Entities.Notifications;

namespace StyleNest.User.API.Services;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>
/// ENH-NOTIF-003 — MSG91 WhatsApp Business channel settings.
/// Bind from appsettings.json section <c>"Msg91WhatsApp"</c>.
/// </summary>
public sealed class Msg91WhatsAppSettings
{
    public const string Section = "Msg91WhatsApp";

    /// <summary>MSG91 auth key (from the MSG91 dashboard).</summary>
    public string AuthKey { get; init; } = string.Empty;

    /// <summary>The WhatsApp Business number registered with MSG91 (e.g. "919XXXXXXXXX").</summary>
    public string IntegratedNumber { get; init; } = string.Empty;

    /// <summary>
    /// Maps notification <see cref="NotificationOutbox.Type"/> values to MSG91 approved template IDs.
    /// Example:  <c>{ "ORDER_PLACED": "644d37ba3fbc2e1d3c2e0921" }</c>
    /// If no mapping exists the message falls back to a plain-text body.
    /// </summary>
    public Dictionary<string, string> TemplateIds { get; init; } = [];
}

// ── Low-level MSG91 client ────────────────────────────────────────────────────

public interface IMsg91WhatsAppClient
{
    /// <summary>
    /// Sends a WhatsApp message to <paramref name="toNumber"/> using the MSG91 API.
    /// Returns <c>true</c> on success (HTTP 2xx + type == "success").
    /// </summary>
    Task<bool> SendAsync(string toNumber, string templateId, string body, CancellationToken ct = default);
}

/// <summary>
/// ENH-NOTIF-003 — MSG91 WhatsApp outbound message client.
///
/// API reference: https://msg91.com/help/whatsapp/how-to-send-whatsapp-message-through-msg91-api
/// Endpoint: POST https://api.msg91.com/api/v5/whatsapp/whatsapp-outbound-message/bulk/
/// </summary>
public sealed class Msg91WhatsAppClient(
    IHttpClientFactory httpFactory,
    Msg91WhatsAppSettings settings,
    ILogger<Msg91WhatsAppClient> logger) : IMsg91WhatsAppClient
{
    private const string BaseUrl    = "https://api.msg91.com/api/v5/whatsapp/whatsapp-outbound-message/bulk/";
    private const string HttpClient = "msg91-wa";

    public async Task<bool> SendAsync(
        string            toNumber,
        string            templateId,
        string            body,
        CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient(HttpClient);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("authkey", settings.AuthKey);

        var payload = new Msg91BulkRequest(
            Integrated_number: settings.IntegratedNumber,
            Content_type:      "template",
            Payload: new Msg91Payload(
                To:         [new Msg91Recipient(toNumber)],
                Type:       "text",
                Template_id: templateId,
                Body:       new Msg91Body(body)
            )
        );

        try
        {
            var response = await client.PostAsJsonAsync(BaseUrl, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "ENH-NOTIF-003: MSG91 WhatsApp API returned {Status} for template={TemplateId}",
                    (int)response.StatusCode, templateId);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<Msg91Response>(cancellationToken: ct);
            var success = result?.Type?.Equals("success", StringComparison.OrdinalIgnoreCase) == true;

            if (!success)
                logger.LogWarning(
                    "ENH-NOTIF-003: MSG91 response type={Type}, message={Message}",
                    result?.Type, result?.Message);

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ENH-NOTIF-003: Exception calling MSG91 WhatsApp API for template={TemplateId}", templateId);
            return false;
        }
    }

    // ── MSG91 request/response DTOs ───────────────────────────────────────────

    private sealed record Msg91BulkRequest(
        [property: JsonPropertyName("integrated_number")] string Integrated_number,
        [property: JsonPropertyName("content_type")]      string Content_type,
        [property: JsonPropertyName("payload")]            Msg91Payload Payload);

    private sealed record Msg91Payload(
        [property: JsonPropertyName("to")]          IReadOnlyList<Msg91Recipient> To,
        [property: JsonPropertyName("type")]         string Type,
        [property: JsonPropertyName("template_id")]  string Template_id,
        [property: JsonPropertyName("body")]         Msg91Body Body);

    private sealed record Msg91Recipient(
        [property: JsonPropertyName("user_whatsapp_number")] string UserWhatsAppNumber);

    private sealed record Msg91Body(
        [property: JsonPropertyName("body_parameter_values")] string BodyParameterValues);

    private sealed class Msg91Response
    {
        [JsonPropertyName("type")]    public string? Type    { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
    }
}

// ── INotificationSender adapter ───────────────────────────────────────────────

/// <summary>
/// ENH-NOTIF-003 — WhatsApp notification sender that adapts <see cref="NotificationOutbox"/>
/// records to MSG91 WhatsApp messages.
///
/// Routing:
///   • Looks up the MSG91 template ID from <see cref="Msg91WhatsAppSettings.TemplateIds"/>
///     keyed by <see cref="NotificationOutbox.Type"/>.
///   • Falls back to a template-less plain-text body if no mapping found.
///   • Only sends when <see cref="NotificationOutbox.Type"/> starts with one of the
///     whitelisted prefixes (ORDER_, RETURN_, OTP_, FLASH_) — all others are skipped
///     (returning <c>false</c> so the retry job marks them as failed and retries via email).
/// </summary>
public sealed class WhatsAppNotificationSender(
    IMsg91WhatsAppClient waClient,
    Msg91WhatsAppSettings settings,
    ILogger<WhatsAppNotificationSender> logger) : INotificationSender
{
    // Only these notification types are routed to WhatsApp
    private static readonly HashSet<string> WhatsAppTypePrefixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ORDER_PLACED", "ORDER_CONFIRMED", "ORDER_SHIPPED",
            "ORDER_DELIVERED", "ORDER_CANCELLED",
            "RETURN_INITIATED", "RETURN_APPROVED",
            "OTP", "FLASH_SALE",
        };

    public async Task<bool> SendAsync(NotificationOutbox item, CancellationToken ct = default)
    {
        // Filter: only WhatsApp-routed types
        if (!IsWhatsAppEligible(item.Type))
        {
            logger.LogDebug(
                "ENH-NOTIF-003: Skipping WhatsApp for type={Type} — not in whitelist", item.Type);
            return false; // Let the retry job try other channels
        }

        // Resolve destination phone number from subject (format: "PHONE:<e164>|<human-readable>")
        var phone = ExtractPhone(item.Subject);
        if (string.IsNullOrWhiteSpace(phone))
        {
            logger.LogWarning(
                "ENH-NOTIF-003: No phone number found in subject for item {Id} type={Type}", item.Id, item.Type);
            return false;
        }

        settings.TemplateIds.TryGetValue(item.Type, out var templateId);
        templateId ??= "generic";

        logger.LogInformation(
            "ENH-NOTIF-003: Sending WhatsApp type={Type} to={Phone} template={TemplateId}",
            item.Type, MaskPhone(phone), templateId);

        return await waClient.SendAsync(phone, templateId, item.Body, ct);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static bool IsWhatsAppEligible(string type) =>
        WhatsAppTypePrefixes.Contains(type) ||
        WhatsAppTypePrefixes.Any(p => type.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Extracts the E.164 phone from subject field.
    /// Convention: subject = "PHONE:919876543210|Order #ORD-..." or plain text.
    /// </summary>
    private static string? ExtractPhone(string subject)
    {
        if (subject.StartsWith("PHONE:", StringComparison.OrdinalIgnoreCase))
        {
            var part = subject["PHONE:".Length..];
            var pipe = part.IndexOf('|');
            return pipe >= 0 ? part[..pipe] : part;
        }
        return null;
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 6
            ? $"{phone[..3]}****{phone[^4..]}"
            : "****";
}
