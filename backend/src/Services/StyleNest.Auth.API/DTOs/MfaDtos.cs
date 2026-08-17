namespace StyleNest.Auth.API.DTOs;

/// <summary>ENH-AUTH-012 — MFA verification request payload.</summary>
public sealed record MfaVerifyRequest(string MfaToken, string OtpCode);
