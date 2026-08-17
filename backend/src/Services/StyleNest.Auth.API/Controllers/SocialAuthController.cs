using Microsoft.AspNetCore.Mvc;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;

namespace StyleNest.Auth.API.Controllers;

/// <summary>
/// ENH-AUTH-003 — Social account merge endpoints.
/// POST /api/v1/auth/social/callback  — handle social login; return MERGE_REQUIRED or NEW_ACCOUNT.
/// POST /api/v1/auth/merge/confirm    — complete merge after password challenge.
///
/// ENH-AUTH-001 — Facebook OAuth 2.0 Login.
/// GET  /api/v1/auth/facebook/url      — returns the Facebook authorization URL.
/// POST /api/v1/auth/facebook/callback — exchanges code for token, returns JWT.
///
/// ENH-AUTH-002 — Apple Sign-In.
/// GET  /api/v1/auth/apple/url      — returns the Apple authorization URL.
/// POST /api/v1/auth/apple/callback — validates Apple id_token, returns JWT.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class SocialAuthController(
    IAccountMergeService mergeService,
    IFacebookAuthService facebookAuth,
    IAppleAuthService    appleAuth) : ControllerBase
{
    // ── ENH-AUTH-001 — Facebook OAuth 2.0 ─────────────────────────────────────

    /// <summary>Returns the Facebook OAuth 2.0 authorization URL.</summary>
    [HttpGet("facebook/url")]
    [ProducesResponseType(typeof(SocialAuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetFacebookUrl(
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { message = "redirectUri is required." });

        var url = facebookAuth.GetAuthorizationUrl(
            redirectUri,
            state ?? Guid.NewGuid().ToString("N"));

        return Ok(new SocialAuthUrlResponse(url));
    }

    /// <summary>
    /// Exchanges a Facebook authorization code for a JWT.
    /// Returns Action=NEW_ACCOUNT (with Auth) or MERGE_REQUIRED (with MergeToken).
    /// </summary>
    [HttpPost("facebook/callback")]
    [ProducesResponseType(typeof(SocialCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FacebookCallback(
        [FromBody] FacebookCallbackRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Code is required." });
        if (string.IsNullOrWhiteSpace(request.RedirectUri))
            return BadRequest(new { message = "RedirectUri is required." });

        try
        {
            var socialRequest = await facebookAuth.ExchangeCodeAsync(
                request.Code, request.RedirectUri, ct);

            var response = await mergeService.HandleSocialCallbackAsync(socialRequest, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── ENH-AUTH-002 — Apple Sign-In ──────────────────────────────────────────

    /// <summary>Returns the Apple OAuth 2.0 authorization URL.</summary>
    [HttpGet("apple/url")]
    [ProducesResponseType(typeof(SocialAuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAppleUrl(
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { message = "redirectUri is required." });

        var url = appleAuth.GetAuthorizationUrl(
            redirectUri,
            state ?? Guid.NewGuid().ToString("N"));

        return Ok(new SocialAuthUrlResponse(url));
    }

    /// <summary>
    /// Validates the Apple id_token JWT and signs in or returns MERGE_REQUIRED.
    /// Returns Action=NEW_ACCOUNT (with Auth) or MERGE_REQUIRED (with MergeToken).
    /// </summary>
    [HttpPost("apple/callback")]
    [ProducesResponseType(typeof(SocialCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AppleCallback(
        [FromBody] AppleCallbackRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { message = "IdToken is required." });

        try
        {
            var socialRequest = await appleAuth.ValidateIdentityTokenAsync(request.IdToken, ct);
            var response      = await mergeService.HandleSocialCallbackAsync(socialRequest, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── ENH-AUTH-003 — Generic social callback (stub provider) ────────────────

    [HttpPost("social/callback")]
    [ProducesResponseType(typeof(SocialCallbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SocialCallback(
        [FromBody] SocialCallbackRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest(new { message = "Provider is required." });
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });
        if (string.IsNullOrWhiteSpace(request.ProviderUserId))
            return BadRequest(new { message = "ProviderUserId is required." });

        try
        {
            var response = await mergeService.HandleSocialCallbackAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("merge/confirm")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmMerge(
        [FromBody] MergeConfirmRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MergeToken))
            return BadRequest(new { message = "MergeToken is required." });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required." });

        try
        {
            var auth = await mergeService.ConfirmMergeAsync(request, ct);
            return Ok(auth);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
