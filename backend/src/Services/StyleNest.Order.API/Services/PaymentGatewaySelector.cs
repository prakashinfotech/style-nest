using Microsoft.Extensions.Options;

namespace StyleNest.Order.API.Services;

/// <summary>ENH-PAY-001 — configuration for primary/fallback gateway and health endpoints.</summary>
public sealed class PaymentGatewaySettings
{
    public const string Section = "PaymentGateway";

    /// <summary>Primary gateway identifier, e.g. "RAZORPAY".</summary>
    public string Primary { get; init; } = "RAZORPAY";

    /// <summary>Fallback gateway identifier, e.g. "PAYU".</summary>
    public string Fallback { get; init; } = "PAYU";

    /// <summary>Gateway name → health-check URL. Empty or absent → gateway assumed healthy.</summary>
    public Dictionary<string, string> HealthCheckUrls { get; init; } = [];

    /// <summary>HTTP timeout for health-check pings (seconds).</summary>
    public int HealthCheckTimeoutSeconds { get; init; } = 3;
}

// ── health-check abstraction ──────────────────────────────────────────────────

public interface IGatewayHealthCheck
{
    /// <summary>Returns true when the named gateway is accepting requests.</summary>
    Task<bool> IsAvailableAsync(string gatewayName, CancellationToken ct = default);
}

/// <summary>
/// ENH-PAY-001 — pings a gateway-specific HTTP status endpoint.
/// Returns true when the endpoint responds 2xx within the configured timeout.
/// Returns true (assume healthy) when no health-check URL is configured.
/// </summary>
public sealed class HttpGatewayHealthCheck(
    IHttpClientFactory httpFactory,
    IOptions<PaymentGatewaySettings> options,
    ILogger<HttpGatewayHealthCheck> logger) : IGatewayHealthCheck
{
    public async Task<bool> IsAvailableAsync(string gatewayName, CancellationToken ct = default)
    {
        if (!options.Value.HealthCheckUrls.TryGetValue(gatewayName, out var url)
            || string.IsNullOrWhiteSpace(url))
        {
            return true;   // no URL configured → optimistic assumption
        }

        try
        {
            using var cts   = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(options.Value.HealthCheckTimeoutSeconds));

            var client   = httpFactory.CreateClient("gateway-health");
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gateway health check failed for {Gateway} at {Url}", gatewayName, url);
            return false;
        }
    }
}

// ── failover selector ─────────────────────────────────────────────────────────

public interface IPaymentGatewaySelector
{
    /// <summary>
    /// ENH-PAY-001 — Returns the active gateway name.
    /// Tries <see cref="PaymentGatewaySettings.Primary"/> first; falls back to
    /// <see cref="PaymentGatewaySettings.Fallback"/> if primary health check fails.
    /// </summary>
    Task<string> SelectAsync(CancellationToken ct = default);
}

/// <summary>
/// ENH-PAY-001 — Selects the payment gateway with automatic failover.
/// Primary → health check → if unavailable → fallback → log PAYMENT_GATEWAY_FAILOVER event.
/// </summary>
public sealed class PaymentGatewaySelector(
    IGatewayHealthCheck healthCheck,
    IOptions<PaymentGatewaySettings> options,
    ILogger<PaymentGatewaySelector> logger) : IPaymentGatewaySelector
{
    public async Task<string> SelectAsync(CancellationToken ct = default)
    {
        var cfg     = options.Value;
        var primary = cfg.Primary;
        var fallback = cfg.Fallback;

        var primaryAvailable = await healthCheck.IsAvailableAsync(primary, ct);
        if (primaryAvailable)
            return primary;

        logger.LogWarning(
            "{EventType} PrimaryGateway={Primary} FallbackGateway={Fallback} Reason=HealthCheckFailed",
            "PAYMENT_GATEWAY_FAILOVER", primary, fallback);

        return fallback;
    }
}
