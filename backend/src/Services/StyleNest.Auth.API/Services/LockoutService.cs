using Microsoft.AspNetCore.Identity;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Auth.API.Services;

/// <summary>
/// ENH-AUTH-005: Error factory for account lockout.
/// Message intentionally stores seconds as a string so the controller can surface
/// `lockoutDurationSeconds` without requiring a subclass of the sealed Error type.
/// </summary>
public static class AuthErrors
{
    public const string AccountLockedCode = "Auth.AccountLocked";

    public static Error AccountLocked(int seconds) =>
        new(AccountLockedCode, seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

public interface ILockoutService
{
    /// <summary>Records a failed attempt; applies exponential doubling if the threshold is reached.</summary>
    /// <returns>Lockout duration in seconds if locked, 0 if not yet locked.</returns>
    Task<int> RecordFailedAttemptAsync(ApplicationUser user, CancellationToken ct = default);

    /// <summary>Resets the failed counter and lockout duration on successful login.</summary>
    Task ResetLockoutAsync(ApplicationUser user, CancellationToken ct = default);

    /// <summary>Returns seconds remaining in the current lockout, or 0 if not locked.</summary>
    Task<int> GetRemainingLockoutSecondsAsync(ApplicationUser user);
}

/// <summary>
/// ENH-AUTH-005 — Exponential lockout doubling.
/// Threshold: 5 failures → first lockout 1800s; each subsequent lockout doubles up to 86400s.
/// </summary>
public sealed class LockoutService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ILogger<LockoutService> logger) : ILockoutService
{
    public const int MaxFailedAttempts  = 5;
    public const int InitialLockoutSeconds = 1800;
    public const int MaxLockoutSeconds  = 86400;

    public async Task<int> RecordFailedAttemptAsync(ApplicationUser user, CancellationToken ct = default)
    {
        await userManager.AccessFailedAsync(user);

        if (!await userManager.IsLockedOutAsync(user))
            return 0;

        // User just got locked out — apply doubling
        var previous = user.LockoutDurationSeconds;
        var next     = previous == 0 ? InitialLockoutSeconds
                                     : Math.Min(previous * 2, MaxLockoutSeconds);

        // Override the default Identity lockout duration with the doubled value
        await userManager.SetLockoutEndDateAsync(
            user, DateTimeOffset.UtcNow + TimeSpan.FromSeconds(next));

        user.LockoutDurationSeconds = next;
        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Account locked for {Email}: {Duration}s (previous: {Previous}s)",
            user.Email, next, previous);

        return next;
    }

    public async Task ResetLockoutAsync(ApplicationUser user, CancellationToken ct = default)
    {
        user.LockoutDurationSeconds = 0;
        await db.SaveChangesAsync(ct);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<int> GetRemainingLockoutSecondsAsync(ApplicationUser user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        if (!lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.UtcNow)
            return 0;

        return (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalSeconds);
    }
}
