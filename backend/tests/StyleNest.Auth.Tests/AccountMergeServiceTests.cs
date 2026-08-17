/**
 * ENH-AUTH-003 — Account Merge: social email matches verified local account.
 * FR-AUTH-009 / BR-AUTH-007 (SOW v2.1)
 * Acceptance criteria:
 *   - Social email matches verified local account → MERGE_REQUIRED + mergeToken
 *   - Social email matches unverified local account → InvalidOperationException
 *   - No local account → NEW_ACCOUNT + JWT
 *   - Merge confirmed with correct password → AddLoginAsync called; JWT returned
 *   - Merge confirmed with wrong password → UnauthorizedAccessException
 *   - Expired/invalid merge token → InvalidOperationException
 *   - Merge token invalidated after successful use (one-time)
 */

using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;
using StyleNest.Infrastructure.Entities.Auth;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class AccountMergeServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly IMemoryCache _cache;
    private readonly AccountMergeService _sut;

    public AccountMergeServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<Guid?>()))
            .Returns("fake-access-token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("fake-refresh-token");
        _tokenServiceMock.Setup(t => t.AccessTokenExpiresAt).Returns(DateTime.UtcNow.AddHours(1));

        _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        _sut = new AccountMergeService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _cache,
            NullLogger<AccountMergeService>.Instance);
    }

    public void Dispose() => _cache.Dispose();

    // ── HandleSocialCallbackAsync ─────────────────────────────────────────────

    [Fact]
    public async Task HandleSocialCallback_VerifiedLocalAccount_ReturnsMergeRequired()
    {
        SetupFindByEmail(BuildUser(emailConfirmed: true));

        var result = await _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest("GOOGLE", "verified@example.com", "g-123"));

        result.Action.Should().Be("MERGE_REQUIRED");
        result.MergeToken.Should().NotBeNullOrEmpty();
        result.Auth.Should().BeNull();
    }

    [Fact]
    public async Task HandleSocialCallback_VerifiedAccount_MergeTokenStoredInCache()
    {
        SetupFindByEmail(BuildUser(emailConfirmed: true));

        var result = await _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest("GOOGLE", "verified@example.com", "g-123"));

        _cache.TryGetValue($"merge:{result.MergeToken}", out object? cached).Should().BeTrue();
        cached.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleSocialCallback_UnverifiedLocalAccount_Throws()
    {
        SetupFindByEmail(BuildUser(emailConfirmed: false));

        var act = () => _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest("GOOGLE", "unverified@example.com", "g-456"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not been verified*");
    }

    [Fact]
    public async Task HandleSocialCallback_NoLocalAccount_ReturnsNewAccount()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);

        var result = await _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest("FACEBOOK", "new@example.com", "fb-789", "New User"));

        result.Action.Should().Be("NEW_ACCOUNT");
        result.Auth.Should().NotBeNull();
        result.Auth!.AccessToken.Should().Be("fake-access-token");
    }

    [Fact]
    public async Task HandleSocialCallback_NoLocalAccount_CallsAddLoginAsync()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);

        await _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest("GOOGLE", "brand@new.com", "g-999"));

        _userManagerMock.Verify(m => m.AddLoginAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<UserLoginInfo>(li => li.LoginProvider == "GOOGLE" && li.ProviderKey == "g-999")),
            Times.Once);
    }

    // ── ConfirmMergeAsync ─────────────────────────────────────────────────────
    // Seed the cache by calling HandleSocialCallbackAsync (which stores a PendingMerge record),
    // then use the returned MergeToken in the confirm tests. This avoids needing to access
    // the private PendingMerge nested record directly.

    [Fact]
    public async Task ConfirmMerge_ValidTokenAndPassword_ReturnsJwt()
    {
        var (user, mergeToken) = await ArrangeMergePending("GOOGLE", "g-001");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct-pass")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var auth = await _sut.ConfirmMergeAsync(new MergeConfirmRequest(mergeToken, "correct-pass"));

        auth.AccessToken.Should().Be("fake-access-token");
        auth.RefreshToken.Should().Be("fake-refresh-token");
    }

    [Fact]
    public async Task ConfirmMerge_ValidTokenAndPassword_CallsAddLoginAsync()
    {
        var (user, mergeToken) = await ArrangeMergePending("GOOGLE", "g-002");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        await _sut.ConfirmMergeAsync(new MergeConfirmRequest(mergeToken, "pass"));

        _userManagerMock.Verify(m => m.AddLoginAsync(
            user,
            It.Is<UserLoginInfo>(li => li.LoginProvider == "GOOGLE" && li.ProviderKey == "g-002")),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmMerge_WrongPassword_ThrowsUnauthorized()
    {
        var (user, mergeToken) = await ArrangeMergePending("GOOGLE", "g-003");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var act = () => _sut.ConfirmMergeAsync(new MergeConfirmRequest(mergeToken, "wrong"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Incorrect password*");
    }

    [Fact]
    public async Task ConfirmMerge_InvalidToken_Throws()
    {
        var act = () => _sut.ConfirmMergeAsync(
            new MergeConfirmRequest("00000000000000000000000000000000", "pass"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid or has expired*");
    }

    [Fact]
    public async Task ConfirmMerge_TokenRemovedFromCacheAfterSuccessfulMerge()
    {
        var (user, mergeToken) = await ArrangeMergePending("GOOGLE", "g-004");
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "pass")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        await _sut.ConfirmMergeAsync(new MergeConfirmRequest(mergeToken, "pass"));

        _cache.TryGetValue($"merge:{mergeToken}", out _).Should().BeFalse();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationUser BuildUser(bool emailConfirmed) => new()
    {
        Id             = Guid.NewGuid(),
        Email          = "verified@example.com",
        UserName       = "verified@example.com",
        EmailConfirmed = emailConfirmed,
        FirstName      = "Test",
        LastName       = "User",
    };

    private void SetupFindByEmail(ApplicationUser user) =>
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

    /// <summary>
    /// Seeds a pending merge in the cache by calling HandleSocialCallbackAsync with a verified user.
    /// Returns the user and the merge token for use in confirm tests.
    /// </summary>
    private async Task<(ApplicationUser user, string mergeToken)> ArrangeMergePending(
        string provider, string providerUserId)
    {
        var user = BuildUser(emailConfirmed: true);
        SetupFindByEmail(user);

        var response = await _sut.HandleSocialCallbackAsync(
            new SocialCallbackRequest(provider, user.Email!, providerUserId));

        return (user, response.MergeToken!);
    }
}
