/**
 * ENH-AUTH-012 — MFA Enforcement for Admin / SuperAdmin (BR-AUTH-006)
 * Acceptance criteria:
 *   - BeginAsync: invalidates prior MFA OTPs, creates new OTP in DB, delivers via channel, returns mfaToken
 *   - CompleteAsync: valid token + correct code → AuthResponseDto with mfa_verified JWT
 *   - CompleteAsync: invalid/expired mfaToken → Auth.MfaTokenExpired error
 *   - CompleteAsync: wrong OTP code → Auth.MfaInvalidCode error
 *   - MfaToken removed from cache after successful completion (one-time use)
 *   - OTP purpose = MfaVerification (enum value 3)
 */

using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StyleNest.Auth.API.Services;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class MfaServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IOtpDeliveryChannel> _deliveryChannelMock;
    private readonly IMemoryCache _cache;
    private readonly MfaService _sut;

    public MfaServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<Guid?>(), It.IsAny<bool>()))
            .Returns("mfa-access-token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("mfa-refresh-token");
        _tokenServiceMock.Setup(t => t.AccessTokenExpiresAt).Returns(DateTime.UtcNow.AddHours(1));

        _deliveryChannelMock = new Mock<IOtpDeliveryChannel>();
        _deliveryChannelMock
            .Setup(d => d.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        _sut = new MfaService(
            _db,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _deliveryChannelMock.Object,
            _cache,
            NullLogger<MfaService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    // ── BeginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BeginAsync_ReturnsMfaToken()
    {
        var user = BuildUser();
        var mfaToken = await _sut.BeginAsync(user);

        mfaToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BeginAsync_StoresMfaTokenInCache()
    {
        var user = BuildUser();
        var mfaToken = await _sut.BeginAsync(user);

        _cache.TryGetValue($"mfa:{mfaToken}", out object? cached).Should().BeTrue();
        cached.Should().Be(user.Id);
    }

    [Fact]
    public async Task BeginAsync_CreatesOtpInDb()
    {
        var user = BuildUser();
        await _sut.BeginAsync(user);

        var otp = await _db.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Email == user.Email && o.Purpose == OtpPurpose.MfaVerification);

        otp.Should().NotBeNull();
        otp!.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task BeginAsync_DeliversOtpViaChannel()
    {
        var user = BuildUser();
        await _sut.BeginAsync(user);

        _deliveryChannelMock.Verify(
            d => d.DeliverAsync(user.Email!, It.IsAny<string>(), "MfaVerification", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginAsync_InvalidatesPriorUnusedOtps()
    {
        var user = BuildUser();
        // Seed an existing unused MFA OTP
        _db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = user.Email!,
            Code      = "111111",
            Purpose   = OtpPurpose.MfaVerification,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
        });
        await _db.SaveChangesAsync();

        await _sut.BeginAsync(user);

        var prior = await _db.OtpCodes
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Code == "111111");
        prior.IsUsed.Should().BeTrue();
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_ValidTokenAndCode_ReturnsJwt()
    {
        var user = BuildUser();
        var (mfaToken, code) = await ArrangePendingMfa(user);

        var result = await _sut.CompleteAsync(mfaToken, code);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("mfa-access-token");
    }

    [Fact]
    public async Task CompleteAsync_ValidTokenAndCode_PassesMfaVerifiedTrue()
    {
        var user = BuildUser();
        var (mfaToken, code) = await ArrangePendingMfa(user);

        await _sut.CompleteAsync(mfaToken, code);

        _tokenServiceMock.Verify(t =>
            t.GenerateAccessToken(
                It.IsAny<ApplicationUser>(),
                It.IsAny<IList<string>>(),
                It.IsAny<Guid?>(),
                true),   // mfaVerified = true
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_InvalidMfaToken_ReturnsMfaTokenExpiredError()
    {
        var result = await _sut.CompleteAsync("nonexistent-token-000000000000000", "123456");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.MfaTokenExpired");
    }

    [Fact]
    public async Task CompleteAsync_WrongOtpCode_ReturnsMfaInvalidCodeError()
    {
        var user = BuildUser();
        var (mfaToken, _) = await ArrangePendingMfa(user);

        var result = await _sut.CompleteAsync(mfaToken, "000000");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.MfaInvalidCode");
    }

    [Fact]
    public async Task CompleteAsync_MfaTokenRemovedFromCacheAfterSuccess()
    {
        var user = BuildUser();
        var (mfaToken, code) = await ArrangePendingMfa(user);

        await _sut.CompleteAsync(mfaToken, code);

        _cache.TryGetValue($"mfa:{mfaToken}", out _).Should().BeFalse();
    }

    // ── OtpPurpose enum ───────────────────────────────────────────────────────

    [Fact]
    public void OtpPurpose_MfaVerification_HasValue3()
        => ((int)OtpPurpose.MfaVerification).Should().Be(3);

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ApplicationUser BuildUser() => new()
    {
        Id        = Guid.NewGuid(),
        Email     = "admin@test.com",
        UserName  = "admin@test.com",
        FirstName = "Admin",
        LastName  = "User",
    };

    private async Task<(string mfaToken, string code)> ArrangePendingMfa(ApplicationUser user)
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var mfaToken = await _sut.BeginAsync(user);

        var otp = await _db.OtpCodes
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Email == user.Email && o.Purpose == OtpPurpose.MfaVerification && !o.IsUsed);

        return (mfaToken, otp.Code);
    }
}
