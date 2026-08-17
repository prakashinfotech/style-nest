namespace StyleNest.Auth.API.DTOs;

/// <summary>ENH-AUTH-003 — Social callback payload (stub; no real SDK token validation).</summary>
public sealed record SocialCallbackRequest(
    string Provider,
    string Email,
    string ProviderUserId,
    string? DisplayName = null);

/// <summary>ENH-AUTH-003 — Merge confirmation payload.</summary>
public sealed record MergeConfirmRequest(
    string MergeToken,
    string Password);

/// <summary>ENH-AUTH-003 — Response returned when a social email matches a verified local account.</summary>
public sealed record SocialCallbackResponse(
    string Action,
    string? MergeToken = null,
    AuthResponseDto? Auth = null);

// ── ENH-AUTH-001 — Facebook OAuth 2.0 ────────────────────────────────────────

/// <summary>ENH-AUTH-001 — Returned by GET /api/v1/auth/facebook/url.</summary>
public sealed record SocialAuthUrlResponse(string Url);

/// <summary>ENH-AUTH-001 — POST /api/v1/auth/facebook/callback body.</summary>
public sealed record FacebookCallbackRequest(string Code, string RedirectUri);

// ── ENH-AUTH-002 — Apple Sign-In ──────────────────────────────────────────────

/// <summary>ENH-AUTH-002 — POST /api/v1/auth/apple/callback body.</summary>
public sealed record AppleCallbackRequest(string IdToken);
