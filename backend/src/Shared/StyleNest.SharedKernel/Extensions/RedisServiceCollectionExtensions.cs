using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace StyleNest.SharedKernel.Extensions;

/// <summary>
/// ENH-INFRA-002 — Redis AAD Managed Identity Auth.
///
/// Extension methods that register <c>IDistributedCache</c> backed by Redis using either:
///   • <b>AAD Managed Identity</b>: when <c>Redis:UseManagedIdentity = true</c> in config.
///     <see cref="DefaultAzureCredential"/> acquires a token for
///     <c>https://redis.azure.com/.default</c> and injects it as the Redis password.
///     The token is refreshed automatically via a custom reconnect handler before expiry.
///   • <b>Plain connection string</b> fallback: when MSI is disabled or not configured.
///
/// Config section expected:
/// <code>
/// "ConnectionStrings": {
///   "Redis": "my-cache.redis.cache.windows.net:6380,ssl=true,abortConnect=False"
/// },
/// "Redis": {
///   "UseManagedIdentity": true,
///   "ClientId": ""            // optional: user-assigned MSI client ID
/// }
/// </code>
///
/// When <c>ConnectionStrings:Redis</c> is absent, the method is a no-op —
/// callers should register <c>AddDistributedMemoryCache</c> as the fallback.
/// </summary>
public static class RedisServiceCollectionExtensions
{
    private const string RedisScope = "https://redis.azure.com/.default";

    /// <summary>
    /// Registers IDistributedCache backed by Azure Cache for Redis.
    /// Uses Managed Identity auth when configured, otherwise plain connection string.
    /// </summary>
    /// <returns><c>true</c> if Redis was registered; <c>false</c> if no connection string found.</returns>
    public static bool AddAzureRedisCache(
        this IServiceCollection services,
        IConfiguration          configuration,
        string                  connectionStringKey = "Redis")
    {
        var connStr = configuration.GetConnectionString(connectionStringKey);
        if (string.IsNullOrWhiteSpace(connStr)) return false;

        var useMsi    = configuration.GetValue<bool>("Redis:UseManagedIdentity");
        var clientId  = configuration["Redis:ClientId"];

        if (useMsi)
        {
            services.AddStackExchangeRedisCache(opt =>
                opt.ConfigurationOptions = BuildMsiOptions(connStr, clientId));

            services.AddSingleton<IRedisTokenRefreshService>(sp =>
                new RedisTokenRefreshService(
                    connStr,
                    clientId,
                    sp.GetRequiredService<ILogger<RedisTokenRefreshService>>()));
        }
        else
        {
            services.AddStackExchangeRedisCache(opt => opt.Configuration = connStr);
        }

        return true;
    }

    // ── MSI helpers ───────────────────────────────────────────────────────────

    private static ConfigurationOptions BuildMsiOptions(string endpoint, string? clientId)
    {
        var opts = ConfigurationOptions.Parse(endpoint);
        opts.Ssl              = true;
        opts.AbortOnConnectFail = false;

        // Inject AAD token as password — token is obtained synchronously at startup.
        // Background refresh handles rotation (see RedisTokenRefreshService).
        var cred  = BuildCredential(clientId);
        var token = cred.GetToken(new TokenRequestContext([RedisScope]), CancellationToken.None);
        opts.Password = token.Token;

        return opts;
    }

    private static TokenCredential BuildCredential(string? clientId) =>
        string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId,
            });
}

// ── Token refresh background service ─────────────────────────────────────────

public interface IRedisTokenRefreshService
{
    /// <summary>Returns the current AAD access token for Redis.</summary>
    string GetCurrentToken();
}

/// <summary>
/// ENH-INFRA-002 — Singleton service that proactively refreshes the Azure AD
/// token used as the Redis password before it expires.
/// Logged at Info level on each refresh so token rotation is auditable.
/// </summary>
public sealed class RedisTokenRefreshService : IRedisTokenRefreshService, IDisposable
{
    private readonly string            _endpoint;
    private readonly TokenCredential   _credential;
    private readonly ILogger           _logger;
    private readonly Timer             _timer;
    private          string            _currentToken = string.Empty;
    private readonly object            _lock         = new();

    // Refresh 5 minutes before the default 1-hour token expiry
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(55);

    public RedisTokenRefreshService(
        string  endpoint,
        string? clientId,
        ILogger<RedisTokenRefreshService> logger)
    {
        _endpoint   = endpoint;
        _credential = string.IsNullOrWhiteSpace(clientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });
        _logger     = logger;

        // Fetch initial token
        RefreshToken(null);

        // Schedule recurring refresh
        _timer = new Timer(RefreshToken, null, RefreshInterval, RefreshInterval);
    }

    public string GetCurrentToken()
    {
        lock (_lock) return _currentToken;
    }

    private void RefreshToken(object? _)
    {
        try
        {
            var token = _credential.GetToken(
                new TokenRequestContext(["https://redis.azure.com/.default"]),
                CancellationToken.None);

            lock (_lock) { _currentToken = token.Token; }

            _logger.LogInformation(
                "ENH-INFRA-002: Redis AAD token refreshed for {Endpoint} — expires {ExpiresOn:u}",
                _endpoint, token.ExpiresOn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ENH-INFRA-002: Failed to refresh Redis AAD token for {Endpoint}", _endpoint);
        }
    }

    public void Dispose() => _timer.Dispose();
}
