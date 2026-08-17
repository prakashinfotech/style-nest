using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using StyleNest.Auth.API.DTOs;
using StyleNest.Infrastructure.Entities.Auth;

namespace StyleNest.Auth.API.Services;

public interface IAccountMergeService
{
    /// <summary>
    /// Processes a social login callback.  Returns MERGE_REQUIRED when the email matches
    /// a verified local account; NEW_ACCOUNT when no local account exists.
    /// </summary>
    Task<SocialCallbackResponse> HandleSocialCallbackAsync(SocialCallbackRequest request, CancellationToken ct = default);

    /// <summary>Confirms account merge after password challenge and links the social identity.</summary>
    Task<AuthResponseDto> ConfirmMergeAsync(MergeConfirmRequest request, CancellationToken ct = default);
}

/// <summary>
/// ENH-AUTH-003 — Account merge: social email matches verified local account.
/// FR-AUTH-009 / BR-AUTH-007 (SOW v2.1).
/// Merge token is stored in IMemoryCache with a 10-minute TTL.
/// Actual OAuth token validation is out-of-scope (stub provider pattern).
/// </summary>
public sealed class AccountMergeService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IMemoryCache cache,
    ILogger<AccountMergeService> logger) : IAccountMergeService
{
    private static readonly TimeSpan MergeTokenTtl = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "merge:";

    public async Task<SocialCallbackResponse> HandleSocialCallbackAsync(
        SocialCallbackRequest request,
        CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.ToUpperInvariant();
        var existing = await userManager.FindByEmailAsync(normalizedEmail);

        if (existing is null)
        {
            // No local account — create one linked to the social identity
            var newUser = new ApplicationUser
            {
                UserName        = request.Email,
                Email           = request.Email,
                EmailConfirmed  = true,
                FirstName       = request.DisplayName?.Split(' ').FirstOrDefault() ?? string.Empty,
                LastName        = request.DisplayName?.Contains(' ') == true
                                      ? request.DisplayName[(request.DisplayName.IndexOf(' ') + 1)..]
                                      : string.Empty,
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create social account: {errors}");
            }

            await userManager.AddLoginAsync(newUser,
                new UserLoginInfo(request.Provider, request.ProviderUserId, request.Provider));

            var roles = await userManager.GetRolesAsync(newUser);
            var auth  = BuildAuthResponse(newUser, roles);

            logger.LogInformation(
                "{EventType} Provider={Provider} UserId={UserId} Email={Email} Action=NEW_ACCOUNT",
                "ACCOUNT_MERGE", request.Provider, newUser.Id, request.Email);

            return new SocialCallbackResponse("NEW_ACCOUNT", Auth: auth);
        }

        if (!existing.EmailConfirmed)
        {
            logger.LogWarning(
                "{EventType} Provider={Provider} Email={Email} Action=UNVERIFIED_LOCAL blocked merge",
                "ACCOUNT_MERGE", request.Provider, request.Email);

            // Cannot auto-merge into an unverified account — surface error
            throw new InvalidOperationException(
                "A local account with this email exists but has not been verified. " +
                "Please verify your email first.");
        }

        // Verified local account found — require password challenge before merging
        var mergeToken = Guid.NewGuid().ToString("N");
        cache.Set(
            $"{CacheKeyPrefix}{mergeToken}",
            new PendingMerge(existing.Id, request.Provider, request.ProviderUserId),
            MergeTokenTtl);

        logger.LogInformation(
            "{EventType} Provider={Provider} UserId={UserId} Email={Email} Action=MERGE_REQUIRED",
            "ACCOUNT_MERGE", request.Provider, existing.Id, request.Email);

        return new SocialCallbackResponse("MERGE_REQUIRED", MergeToken: mergeToken);
    }

    public async Task<AuthResponseDto> ConfirmMergeAsync(
        MergeConfirmRequest request,
        CancellationToken ct = default)
    {
        if (!cache.TryGetValue($"{CacheKeyPrefix}{request.MergeToken}", out PendingMerge? pending)
            || pending is null)
        {
            throw new InvalidOperationException("Merge token is invalid or has expired.");
        }

        var user = await userManager.FindByIdAsync(pending.UserId.ToString())
            ?? throw new InvalidOperationException("User not found for merge confirmation.");

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Incorrect password. Merge cancelled.");

        // Link the social identity to the existing local account
        var loginInfo = new UserLoginInfo(pending.Provider, pending.ProviderUserId, pending.Provider);
        var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);

        if (!addLoginResult.Succeeded)
        {
            var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to link social identity: {errors}");
        }

        // Invalidate the merge token immediately after use
        cache.Remove($"{CacheKeyPrefix}{request.MergeToken}");

        logger.LogInformation(
            "{EventType} Provider={Provider} UserId={UserId} Action=MERGE_CONFIRMED",
            "ACCOUNT_MERGE", pending.Provider, user.Id);

        var roles = await userManager.GetRolesAsync(user);
        return BuildAuthResponse(user, roles);
    }

    private AuthResponseDto BuildAuthResponse(ApplicationUser user, IList<string> roles) =>
        new(
            tokenService.GenerateAccessToken(user, roles),
            tokenService.GenerateRefreshToken(),
            tokenService.AccessTokenExpiresAt,
            new UserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName));

    private sealed record PendingMerge(Guid UserId, string Provider, string ProviderUserId);
}
