using System.Security.Cryptography;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace StyleNest.SharedKernel.Jwt;

/// <summary>
/// ENH-AUTH-006 — Azure Key Vault HSM Pool: loads the RSA-3072+ public key from
/// an Azure Key Vault Premium (HSM-backed) key and exposes it as a
/// <see cref="SecurityKey"/> for JWT Bearer validation.
///
/// Configuration (appsettings.json / Key Vault App Config reference):
/// <code>
/// "Jwt": {
///   "KeyVaultUri":     "https://kv-stylenest-XXXX.vault.azure.net/",
///   "KeyVaultKeyName": "stylenest-jwt-rsa3072",
///   "KeyVaultKeyId":   ""   // optional — pin to a specific version
/// }
/// </code>
///
/// When <c>Jwt:KeyVaultUri</c> is absent or begins with "REPLACE", the class
/// falls back to the standard PEM-based <see cref="JwksKeyProvider"/> behaviour
/// so that local development continues to work without Azure credentials.
///
/// Token re-use: the key is cached in <see cref="IMemoryCache"/> for 15 minutes
/// and is lazily refreshed on the next cache miss — consistent with the existing
/// Polly-backed JWKS refresh strategy.
/// </summary>
public sealed class KeyVaultRsaKeyProvider : IJwksKeyProvider
{
    private const string CacheKey = "kv-jwt-rsa-key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly IConfiguration  _config;
    private readonly IMemoryCache    _cache;
    private readonly ILogger<KeyVaultRsaKeyProvider> _logger;

    // Fallback provider used when KV is not configured
    private readonly IJwksKeyProvider _fallback;

    public KeyVaultRsaKeyProvider(
        IConfiguration   config,
        IMemoryCache     cache,
        IHttpClientFactory httpClientFactory,
        ILogger<KeyVaultRsaKeyProvider> logger,
        ILogger<JwksKeyProvider> fallbackLogger)
    {
        _config   = config;
        _cache    = cache;
        _logger   = logger;
        _fallback = new JwksKeyProvider(config, cache, httpClientFactory, fallbackLogger);
    }

    public IEnumerable<SecurityKey> GetSigningKeys()
    {
        var kvUri     = _config["Jwt:KeyVaultUri"];
        var keyName   = _config["Jwt:KeyVaultKeyName"] ?? "stylenest-jwt-rsa3072";
        var keyId     = _config["Jwt:KeyVaultKeyId"]; // optional version pin

        if (string.IsNullOrWhiteSpace(kvUri) || kvUri.StartsWith("REPLACE"))
        {
            _logger.LogDebug(
                "ENH-AUTH-006: Jwt:KeyVaultUri not configured — using PEM/JWKS fallback.");
            return _fallback.GetSigningKeys();
        }

        if (_cache.TryGetValue<IEnumerable<SecurityKey>>(CacheKey, out var cached) && cached is not null)
            return cached;

        try
        {
            var keys = LoadFromKeyVault(kvUri, keyName, keyId);
            _cache.Set(CacheKey, keys, CacheTtl);
            _logger.LogInformation(
                "ENH-AUTH-006: RSA-HSM public key loaded from KV {KeyVaultUri}, key={KeyName}",
                kvUri, keyName);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ENH-AUTH-006: Failed to load RSA key from Key Vault {KeyVaultUri} — falling back to PEM.",
                kvUri);
            return _fallback.GetSigningKeys();
        }
    }

    // ── Key Vault loading ─────────────────────────────────────────────────────

    private IEnumerable<SecurityKey> LoadFromKeyVault(
        string  kvUri,
        string  keyName,
        string? keyId)
    {
        var credential = new DefaultAzureCredential();
        var keyClient  = new KeyClient(new Uri(kvUri), credential);

        KeyVaultKey key = string.IsNullOrWhiteSpace(keyId)
            ? keyClient.GetKey(keyName)
            : keyClient.GetKey(keyName, keyId);

        var jwk     = key.Key;
        var rsa     = jwk.ToRSA(includePrivateParameters: false);
        var rsaKey  = new RsaSecurityKey(rsa)
        {
            KeyId = key.Id.ToString(),
        };

        return [rsaKey];
    }
}
