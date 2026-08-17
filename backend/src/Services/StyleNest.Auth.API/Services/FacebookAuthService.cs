using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StyleNest.Auth.API.DTOs;

namespace StyleNest.Auth.API.Services;

/// <summary>
/// ENH-AUTH-001 — Facebook OAuth 2.0 service.
/// Builds the authorization URL and exchanges the auth code for a user profile
/// using the Facebook Graph API v18.0.
/// </summary>
public interface IFacebookAuthService
{
    /// <summary>
    /// Returns the Facebook OAuth 2.0 authorization URL.
    /// The caller redirects the user's browser to this URL.
    /// </summary>
    string GetAuthorizationUrl(string redirectUri, string state);

    /// <summary>
    /// Exchanges the authorization code for a Facebook access token,
    /// fetches the user's id/name/email from the Graph API, and returns
    /// a <see cref="SocialCallbackRequest"/> ready for <see cref="IAccountMergeService"/>.
    /// </summary>
    Task<SocialCallbackRequest> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default);
}

public sealed class FacebookAuthService(
    HttpClient http,
    IConfiguration config,
    ILogger<FacebookAuthService> logger) : IFacebookAuthService
{
    private string AppId     => config["Facebook:AppId"]     ?? throw new InvalidOperationException("Facebook:AppId not configured.");
    private string AppSecret => config["Facebook:AppSecret"] ?? throw new InvalidOperationException("Facebook:AppSecret not configured.");
    private string ApiVersion => config["Facebook:GraphApiVersion"] ?? "v18.0";

    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        var encodedRedirect = Uri.EscapeDataString(redirectUri);
        var encodedState    = Uri.EscapeDataString(state);
        return $"https://www.facebook.com/dialog/oauth" +
               $"?client_id={AppId}" +
               $"&redirect_uri={encodedRedirect}" +
               $"&scope=email" +
               $"&response_type=code" +
               $"&state={encodedState}";
    }

    public async Task<SocialCallbackRequest> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default)
    {
        // ── Step 1: Exchange code for access token ──────────────────────────
        var tokenUrl =
            $"https://graph.facebook.com/{ApiVersion}/oauth/access_token" +
            $"?client_id={AppId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&client_secret={AppSecret}" +
            $"&code={Uri.EscapeDataString(code)}";

        var tokenResp = await http.GetFromJsonAsync<FbTokenResponse>(tokenUrl, ct)
            ?? throw new InvalidOperationException("Facebook token exchange returned an empty response.");

        if (string.IsNullOrEmpty(tokenResp.AccessToken))
            throw new InvalidOperationException("Facebook token exchange did not return an access_token.");

        // ── Step 2: Fetch user profile from Graph API ───────────────────────
        var profileUrl =
            $"https://graph.facebook.com/{ApiVersion}/me" +
            $"?fields=id,name,email" +
            $"&access_token={Uri.EscapeDataString(tokenResp.AccessToken)}";

        var profile = await http.GetFromJsonAsync<FbUserProfile>(profileUrl, ct)
            ?? throw new InvalidOperationException("Facebook Graph API returned an empty user profile.");

        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            logger.LogWarning(
                "ENH-AUTH-001 FacebookId={FacebookId} returned no email — login blocked.",
                profile.Id);
            throw new InvalidOperationException(
                "Your Facebook account does not have a verified email address. " +
                "Please add an email to your Facebook account or use email/password login.");
        }

        logger.LogInformation(
            "ENH-AUTH-001 Facebook code exchanged successfully. FacebookId={FacebookId}",
            profile.Id);

        return new SocialCallbackRequest(
            Provider:       "Facebook",
            Email:          profile.Email,
            ProviderUserId: profile.Id,
            DisplayName:    profile.Name);
    }

    // ── Internal Graph API response models ────────────────────────────────────

    private sealed class FbTokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("token_type")]   public string TokenType   { get; init; } = string.Empty;
        [JsonPropertyName("expires_in")]   public int    ExpiresIn   { get; init; }
    }

    private sealed class FbUserProfile
    {
        [JsonPropertyName("id")]    public string Id    { get; init; } = string.Empty;
        [JsonPropertyName("name")]  public string Name  { get; init; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;
    }
}
