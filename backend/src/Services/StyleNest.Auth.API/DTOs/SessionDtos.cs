namespace StyleNest.Auth.API.DTOs;

/// <summary>ENH-AUTH-004 — Active session summary returned by GET /api/v1/auth/sessions.</summary>
public sealed record SessionDto(
    Guid SessionId,
    string? DeviceName,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsCurrent);
