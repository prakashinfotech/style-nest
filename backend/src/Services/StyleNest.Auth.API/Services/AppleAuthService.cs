using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StyleNest.Auth.API.DTOs;

namespace StyleNest.Auth.API.Services;

/// <summary>
/// ENH-AUTH-002 — Apple Sign-In service.
/// Builds the Apple OAuth 2.0 authorization URL (response_type=id_token,
/// response_mode=fragment) and validates the identity token JWT that Apple
/// returns in the URL fragment after user consent.
///
/// Validation uses Apple's public JWKS at https://appleid.apple.com/auth/keys —
/// no client secret is required for id_token verification.
///
/// Hidden-email support: Apple may return a private relay address
/// (…@privaterelay.appleid.com); we accept any non-empty email and let the
/// AccountMergeService decide whether to link or create a new account.
/// </summary>
public interface IAppleAuthService
{
    /// <summary>Returns the Apple OAuth 2.0 authorization URL.</summary>
    string GetAuthorizationUrl(string redirectUri, string state);

    /// <summary>
    /// Validates the Apple identity token, extracts the stable sub + email,
    /// and returns a <see cref="SocialCallbackRequest"/> ready for
    /// <see cref="IAccountMergeService"/>.
    /// Throws <see cref="InvalidOperationException"/> on invalid token or missing email.
    /// </summary>
    Task<SocialCallbackRequest> ValidateIdentityTokenAsync(
        string identityToken, CancellationToken ct = default);
}

public sealed class AppleAuthService(
    HttpClient http,
    IConfiguration config,
    ILogger<AppleAuthService> logger) : IAppleAuthService
{
    private const string AppleIssuer  = "https://appleid.apple.com";
    private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    private string ClientId => config["Apple:ClientId"]
        ?? throw new InvalidOperationException("Apple:ClientId not configured.");

    /// <inheritdoc />
    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        var encoded      = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        // response_mode=fragment — Apple places id_token in the URL hash (#id_token=…)
        // so it is never sent to any server; the SPA reads and forwards only the token.
        return "https://appleid.apple.com/auth/authorize" +
               $"?client_id={ClientId}" +
               $"&redirect_uri={encoded}" +
               $"&response_type=id_token" +
               $"&scope=email" +
               $"&response_mode=fragment" +
               $"&state={encodedState}";
    }

    /// <inheritdoc />
    public async Task<SocialCallbackRequest> ValidateIdentityTokenAsync(
        string identityToken,
        CancellationToken ct = default)
    {
        // ── Step 1: Fetch Apple's public JSON Web Key Set ──────────────────
        var jwksJson = await http.GetStringAsync(AppleJwksUrl, ct)
            ?? throw new InvalidOperationException("Apple JWKS endpoint returned an empty response.");

        var jwks         = new JsonWebKeySet(jwksJson);
        var signingKeys  = jwks.GetSigningKeys();

        // ── Step 2: Validate the identity token signature + claims ─────────
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer              = AppleIssuer,
            ValidAudience            = ClientId,
            IssuerSigningKeys        = signingKeys,
            ValidateIssuerSigningKey = true,
            ValidateLifetime         = true,
        };

        var handler = new JsonWebTokenHandler();
        TokenValidationResult validationResult;
        try
        {
            validationResult = await handler.ValidateTokenAsync(identityToken, validationParameters);
        }
        catch (Exception ex)
        {
            logger.LogWarning("ENH-AUTH-002 Apple token validation threw: {Msg}", ex.Message);
            throw new InvalidOperationException("Apple identity token validation failed.", ex);
        }

        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "ENH-AUTH-002 Apple token is invalid: {Msg}",
                validationResult.Exception?.Message);
            throw new InvalidOperationException("Apple identity token is invalid or expired.");
        }

        // ── Step 3: Extract stable subject identifier and email ────────────
        var identity = validationResult.ClaimsIdentity;

        var sub = identity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? identity.FindFirst("sub")?.Value
                  ?? throw new InvalidOperationException("Apple identity token missing 'sub' claim.");

        // Apple may return a real email or a private relay address (@privaterelay.appleid.com).
        // Both are accepted — AccountMergeService links on email regardless of domain.
        var email = identity.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("ENH-AUTH-002 AppleSub={Sub} provided no email.", sub);
            throw new InvalidOperationException(
                "Your Apple account does not share an email address. " +
                "Please allow email sharing in your Apple ID settings or use email/password login.");
        }

        logger.LogInformation(
            "ENH-AUTH-002 Apple identity token validated. Sub={Sub} IsRelay={IsRelay}",
            sub,
            email.EndsWith("privaterelay.appleid.com", StringComparison.OrdinalIgnoreCase));

        // Apple only surfaces the user's name on the very first consent screen
        // (in the form_post body, not the id_token).  We always set DisplayName=null;
        // AccountMergeService will derive a display name from the email prefix if needed.
        return new SocialCallbackRequest(
            Provider:       "Apple",
            Email:          email,
            ProviderUserId: sub,
            DisplayName:    null);
    }
}
