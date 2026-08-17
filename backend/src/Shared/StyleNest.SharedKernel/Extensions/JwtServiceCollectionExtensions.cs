using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StyleNest.SharedKernel.Jwt;

namespace StyleNest.SharedKernel.Extensions;

public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication backed by a Polly-retry + 15-minute-cached key provider.
    /// Reads Jwt:Issuer, Jwt:Audience, Jwt:PublicKey (PEM) and optionally Jwt:JwksUri from config.
    /// </summary>
    public static IServiceCollection AddResilientJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient();

        // ENH-AUTH-006 — Use Key Vault RSA-HSM key provider in production;
        // fall back to PEM/JWKS provider when Jwt:KeyVaultUri is not configured.
        var kvUri = configuration["Jwt:KeyVaultUri"];
        if (!string.IsNullOrWhiteSpace(kvUri) && !kvUri.StartsWith("REPLACE"))
            services.AddSingleton<IJwksKeyProvider, KeyVaultRsaKeyProvider>();
        else
            services.AddSingleton<IJwksKeyProvider, JwksKeyProvider>();

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = configuration["Jwt:Issuer"],
                ValidAudience            = configuration["Jwt:Audience"],
                ClockSkew                = TimeSpan.Zero
            };
        });

        // Post-configure injects the cached key resolver after all other JWT configuration
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(sp =>
        {
            var provider = sp.GetRequiredService<IJwksKeyProvider>();
            return new PostConfigureOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opt => opt.TokenValidationParameters.IssuerSigningKeyResolver =
                    (_, _, _, _) => provider.GetSigningKeys());
        });

        return services;
    }
}
