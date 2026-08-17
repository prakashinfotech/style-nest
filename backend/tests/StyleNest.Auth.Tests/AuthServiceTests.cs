using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StyleNest.Auth.API.DTOs;
using StyleNest.Auth.API.Services;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AppDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(
                It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>(), It.IsAny<Guid?>()))
            .Returns("fake-access-token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("fake-refresh-token");
        _tokenServiceMock.Setup(t => t.AccessTokenExpiresAt)
            .Returns(DateTime.UtcNow.AddHours(1));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        var lockout = new Mock<ILockoutService>();
        lockout.Setup(l => l.GetRemainingLockoutSecondsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(0);
        lockout.Setup(l => l.RecordFailedAttemptAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        lockout.Setup(l => l.ResetLockoutAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Non-admin users — MFA.BeginAsync never called; CompleteAsync never called in AuthServiceTests
        var mfa = new Mock<IMfaService>();

        _sut = new AuthService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _db,
            NullLogger<AuthService>.Instance,
            httpContextAccessor.Object,
            lockout.Object,
            mfa.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), Email = "user@test.com",
            FirstName = "Test", LastName = "User"
        };
        var dto = new LoginRequestDto("user@test.com", "Password@123");

        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        var result = await _sut.LoginAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("fake-access-token");
        result.Value.User.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailureResult()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@test.com" };
        var dto = new LoginRequestDto("user@test.com", "wrongpassword");

        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(false);

        var result = await _sut.LoginAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailureResult()
    {
        var dto = new LoginRequestDto("nobody@test.com", "Password@123");
        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.LoginAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task RegisterAsync_NewUser_CreatesUserAndReturnsToken()
    {
        var dto = new RegisterRequestDto("Jane", "Doe", "jane@test.com", "Password@123", "Password@123");

        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("fake-access-token");
        result.Value.User.Email.Should().Be("jane@test.com");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailureResult()
    {
        var existing = new ApplicationUser { Id = Guid.NewGuid(), Email = "taken@test.com" };
        var dto = new RegisterRequestDto("John", "Doe", "taken@test.com", "Password@123", "Password@123");

        _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(existing);

        var result = await _sut.RegisterAsync(dto);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Conflict");
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewAccessToken()
    {
        var user = new ApplicationUser
        {
            Id        = Guid.NewGuid(),
            UserName  = "user@test.com",
            Email     = "user@test.com",
            FirstName = "Test",
            LastName  = "User"
        };
        _db.Users.Add(user);

        var refreshToken = new StyleNest.Infrastructure.Entities.Auth.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = "valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        var result = await _sut.RefreshAsync("valid-refresh-token");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("fake-access-token");
    }

    [Fact]
    public async Task RefreshAsync_NonExistentToken_ReturnsFailure()
    {
        var result = await _sut.RefreshAsync("nonexistent-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ReturnsFailure()
    {
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = "revoked@test.com",
            Email    = "revoked@test.com"
        };
        _db.Users.Add(user);

        _db.RefreshTokens.Add(new StyleNest.Infrastructure.Entities.Auth.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = true
        });
        await _db.SaveChangesAsync();

        var result = await _sut.RefreshAsync("revoked-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsFailure()
    {
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid(),
            UserName = "expired@test.com",
            Email    = "expired@test.com"
        };
        _db.Users.Add(user);

        _db.RefreshTokens.Add(new StyleNest.Infrastructure.Entities.Auth.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        });
        await _db.SaveChangesAsync();

        var result = await _sut.RefreshAsync("expired-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidRefreshToken");
    }
}
