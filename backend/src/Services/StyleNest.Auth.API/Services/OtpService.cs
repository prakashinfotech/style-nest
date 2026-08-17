using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Auth.API.Services;

public class OtpService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IOtpDeliveryChannel deliveryChannel,
    ISmsDeliveryChannel smsChannel,
    ILogger<OtpService> logger) : IOtpService
{
    private const int PhoneOtpTtlSeconds = 300;
    private const int PhoneOtpMaxPerHour = 5;

    // ── Email-based password-reset flow ──────────────────────────────────────

    public async Task<Result> SendForgotPasswordOtpAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Success(); // Don't reveal if email exists

        var existing = await db.OtpCodes
            .IgnoreQueryFilters()
            .Where(o => o.Email == email && o.Purpose == OtpPurpose.PasswordReset && !o.IsUsed)
            .ToListAsync();
        existing.ForEach(o => o.IsUsed = true);

        var code = GenerateCode();
        db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = email,
            Code      = code,
            Purpose   = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        });

        await db.SaveChangesAsync();
        await deliveryChannel.DeliverAsync(email, code, nameof(OtpPurpose.PasswordReset));
        logger.LogInformation("Password reset OTP dispatched for {Email}", email);

        return Result.Success();
    }

    public async Task<Result> VerifyOtpAsync(string email, string code, string purpose)
    {
        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var purposeEnum))
            return Result.Failure(new Error("OTP.InvalidPurpose", "Invalid OTP purpose."));

        var otp = await db.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.Email == email &&
                o.Code  == code &&
                o.Purpose == purposeEnum &&
                !o.IsUsed);

        if (otp is null)
            return Result.Failure(new Error("OTP.Invalid", "Invalid or expired OTP."));

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await db.SaveChangesAsync();
            return Result.Failure(new Error("AUTH_OTP_EXPIRED", "OTP has expired."));
        }

        // Mark as used — single-use enforcement (BR-AUTH-001)
        otp.IsUsed = true;
        await db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var otp = await db.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.Email == email &&
                o.Code  == code &&
                o.Purpose == OtpPurpose.PasswordReset &&
                !o.IsUsed);

        if (otp is null)
            return Result.Failure(new Error("OTP.Invalid", "Invalid or expired OTP."));

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await db.SaveChangesAsync();
            return Result.Failure(new Error("AUTH_OTP_EXPIRED", "OTP has expired."));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.Failure(new Error("User.NotFound", "User not found."));

        var token  = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Identity.Error", msg));
        }

        otp.IsUsed = true;
        await db.SaveChangesAsync();

        return Result.Success();
    }

    // ── Phone-based OTP flow (FR-AUTH-001, BR-AUTH-001, EC-AUTH-001, EC-AUTH-002, EC-AUTH-012) ──

    public async Task<Result<PhoneOtpSentResult>> SendPhoneOtpAsync(string phoneNumber)
    {
        // EC-AUTH-012: max 5 OTPs per hour per mobile
        var hourAgo      = DateTime.UtcNow.AddHours(-1);
        var recentCount  = await db.OtpCodes
            .IgnoreQueryFilters()
            .CountAsync(o =>
                o.PhoneNumber == phoneNumber &&
                o.Purpose     == OtpPurpose.PhoneVerification &&
                o.CreatedAt   >= hourAgo);

        if (recentCount >= PhoneOtpMaxPerHour)
            return Result.Failure<PhoneOtpSentResult>(
                new Error("OTP.RateLimitExceeded", "Too many OTP requests. Try again later."));

        // Invalidate any existing unused phone OTPs for this number
        var existing = await db.OtpCodes
            .IgnoreQueryFilters()
            .Where(o =>
                o.PhoneNumber == phoneNumber &&
                o.Purpose     == OtpPurpose.PhoneVerification &&
                !o.IsUsed)
            .ToListAsync();
        existing.ForEach(o => o.IsUsed = true);

        var code      = GenerateCode();
        var expiresAt = DateTime.UtcNow.AddSeconds(PhoneOtpTtlSeconds);

        db.OtpCodes.Add(new OtpCode
        {
            Id          = Guid.NewGuid(),
            Email       = string.Empty,
            PhoneNumber = phoneNumber,
            Code        = code,
            Purpose     = OtpPurpose.PhoneVerification,
            ExpiresAt   = expiresAt,
            CreatedAt   = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        await smsChannel.DeliverAsync(phoneNumber, code);
        logger.LogInformation("Phone OTP dispatched for {Phone}", phoneNumber);

        return Result.Success<PhoneOtpSentResult>(
            new PhoneOtpSentResult(MaskPhone(phoneNumber), expiresAt));
    }

    public async Task<Result> VerifyPhoneOtpAsync(string phoneNumber, string code)
    {
        var otp = await db.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.PhoneNumber == phoneNumber &&
                o.Code        == code &&
                o.Purpose     == OtpPurpose.PhoneVerification &&
                !o.IsUsed);

        if (otp is null)
            return Result.Failure(new Error("OTP.Invalid", "Invalid or expired OTP."));

        if (otp.ExpiresAt < DateTime.UtcNow)
        {
            otp.IsUsed = true;
            await db.SaveChangesAsync();
            return Result.Failure(new Error("AUTH_OTP_EXPIRED", "OTP has expired."));
        }

        // Single-use enforcement (BR-AUTH-001)
        otp.IsUsed = true;
        await db.SaveChangesAsync();

        return Result.Success();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateCode() =>
        Random.Shared.Next(100000, 999999).ToString();

    // Produces +91-XXX-XXX-NNNN from +91XXXXXXXXXX (FR-AUTH-001)
    private static string MaskPhone(string phone)
    {
        var digits = phone.StartsWith("+91", StringComparison.Ordinal) ? phone[3..] : phone;
        return $"+91-XXX-XXX-{digits[^4..]}";
    }
}
