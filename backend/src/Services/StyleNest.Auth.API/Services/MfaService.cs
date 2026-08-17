using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StyleNest.Auth.API.DTOs;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Auth.API.Services;

public interface IMfaService
{
    /// <summary>Sends an MFA OTP and returns a short-lived session token (mfaToken).</summary>
    Task<string> BeginAsync(ApplicationUser user, CancellationToken ct = default);

    /// <summary>Verifies the OTP; on success returns a JWT with mfa_verified=true.</summary>
    Task<Result<AuthResponseDto>> CompleteAsync(string mfaToken, string otpCode, CancellationToken ct = default);
}

/// <summary>
/// ENH-AUTH-012 — MFA enforcement for Admin / SuperAdmin (BR-AUTH-006).
/// OTPs stored in OtpCodes table (purpose=MfaVerification, 5-min TTL).
/// Pending session keyed in IMemoryCache: "mfa:{token}" → userId (5-min TTL).
/// </summary>
public sealed class MfaService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IOtpDeliveryChannel deliveryChannel,
    IMemoryCache cache,
    ILogger<MfaService> logger) : IMfaService
{
    private static readonly TimeSpan OtpTtl         = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SessionTtl     = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix             = "mfa:";
    private const string OtpPurposeLabel            = "MfaVerification";

    public async Task<string> BeginAsync(ApplicationUser user, CancellationToken ct = default)
    {
        // Invalidate any existing MFA OTPs for this email
        var existing = await db.OtpCodes
            .IgnoreQueryFilters()
            .Where(o => o.Email == user.Email && o.Purpose == OtpPurpose.MfaVerification && !o.IsUsed)
            .ToListAsync(ct);
        existing.ForEach(o => o.IsUsed = true);

        // Generate and store new OTP
        var code = GenerateCode();
        db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = user.Email!,
            Code      = code,
            Purpose   = OtpPurpose.MfaVerification,
            ExpiresAt = DateTime.UtcNow.Add(OtpTtl),
        });
        await db.SaveChangesAsync(ct);

        // Deliver OTP
        await deliveryChannel.DeliverAsync(user.Email!, code, OtpPurposeLabel, ct);

        // Store pending session in cache
        var mfaToken = Guid.NewGuid().ToString("N");
        cache.Set($"{CacheKeyPrefix}{mfaToken}", user.Id, SessionTtl);

        logger.LogInformation(
            "MFA challenge started for user {UserId} ({Email})", user.Id, user.Email);

        return mfaToken;
    }

    public async Task<Result<AuthResponseDto>> CompleteAsync(
        string mfaToken, string otpCode, CancellationToken ct = default)
    {
        if (!cache.TryGetValue($"{CacheKeyPrefix}{mfaToken}", out Guid userId))
            return Result.Failure<AuthResponseDto>(
                new Error("Auth.MfaTokenExpired", "MFA session has expired. Please log in again."));

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<AuthResponseDto>(
                new Error("Auth.UserNotFound", "User not found."));

        // Verify OTP
        var otp = await db.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.Email   == user.Email &&
                o.Code    == otpCode &&
                o.Purpose == OtpPurpose.MfaVerification &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow, ct);

        if (otp is null)
            return Result.Failure<AuthResponseDto>(
                new Error("Auth.MfaInvalidCode", "MFA code is invalid or has expired."));

        otp.IsUsed = true;
        await db.SaveChangesAsync(ct);
        cache.Remove($"{CacheKeyPrefix}{mfaToken}");

        logger.LogInformation(
            "MFA completed for user {UserId} ({Email})", user.Id, user.Email);

        var roles = await userManager.GetRolesAsync(user);
        return Result.Success(new AuthResponseDto(
            tokenService.GenerateAccessToken(user, roles, mfaVerified: true),
            tokenService.GenerateRefreshToken(),
            tokenService.AccessTokenExpiresAt,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName)));
    }

    private static string GenerateCode()
    {
        var n = RandomNumberGenerator.GetInt32(1_000_000);
        return n.ToString("D6");
    }
}
