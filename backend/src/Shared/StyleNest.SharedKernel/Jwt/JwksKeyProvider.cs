using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Retry;

namespace StyleNest.SharedKernel.Jwt;

public sealed class JwksKeyProvider : IJwksKeyProvider
{
    private const string CacheKey = "jwks-signing-keys";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JwksKeyProvider> _logger;
    private readonly string? _jwksUri;
    private readonly ResiliencePipeline _retryPipeline;

    private volatile bool _isRefreshing;

    public JwksKeyProvider(
        IConfiguration configuration,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<JwksKeyProvider> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jwksUri = configuration["Jwt:JwksUri"];

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .Build();
    }

    public IEnumerable<SecurityKey> GetSigningKeys()
    {
        if (_cache.TryGetValue<IEnumerable<SecurityKey>>(CacheKey, out var cached) && cached is not null)
            return cached;

        // Cache miss — build from config PEM immediately (zero-latency fallback)
        var keys = BuildFromConfigPem();
        _cache.Set(CacheKey, keys, CacheTtl);

        // If a JWKS endpoint is configured, refresh in background so next expiry uses live keys
        if (!string.IsNullOrEmpty(_jwksUri))
            TriggerBackgroundRefresh();

        return keys;
    }

    private IEnumerable<SecurityKey> BuildFromConfigPem()
    {
        var pem = _configuration["Jwt:PublicKey"]
            ?? throw new InvalidOperationException("Jwt:PublicKey not configured.");
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return [new RsaSecurityKey(rsa)];
    }

    private void TriggerBackgroundRefresh()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        _ = Task.Run(async () =>
        {
            try { await RefreshFromJwksAsync(); }
            finally { _isRefreshing = false; }
        });
    }

    private async Task RefreshFromJwksAsync()
    {
        try
        {
            var json = await _retryPipeline.ExecuteAsync(async ct =>
            {
                var client = _httpClientFactory.CreateClient("jwks");
                return await client.GetStringAsync(_jwksUri, ct);
            });

            var keySet = new JsonWebKeySet(json);
            var keys = keySet.GetSigningKeys();
            _cache.Set(CacheKey, keys, CacheTtl);
            _logger.LogInformation("JWKS signing keys refreshed from {Uri}", _jwksUri);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWKS refresh from {Uri} failed after retries; cached keys retained", _jwksUri);
        }
    }
}
