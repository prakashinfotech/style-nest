using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StyleNest.Auth.API.Services;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class OtpServiceTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpDeliveryChannel> _deliveryChannelMock;
    private readonly Mock<ISmsDeliveryChannel> _smsChannelMock;
    private readonly AppDbContext _db;
    private readonly OtpService _sut;

    public OtpServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _deliveryChannelMock = new Mock<IOtpDeliveryChannel>();
        _deliveryChannelMock
            .Setup(c => c.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smsChannelMock = new Mock<ISmsDeliveryChannel>();
        _smsChannelMock
            .Setup(c => c.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _sut = new OtpService(_db, _userManagerMock.Object, _deliveryChannelMock.Object, _smsChannelMock.Object, NullLogger<OtpService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SendForgotPasswordOtpAsync_UserNotFound_ReturnsSuccessWithoutCreatingOtp()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("nobody@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.SendForgotPasswordOtpAsync("nobody@test.com");

        result.IsSuccess.Should().BeTrue();
        _db.OtpCodes.IgnoreQueryFilters().Count().Should().Be(0);
    }

    [Fact]
    public async Task SendForgotPasswordOtpAsync_UserExists_CreatesOtpCode()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

        var result = await _sut.SendForgotPasswordOtpAsync("user@test.com");

        result.IsSuccess.Should().BeTrue();
        var otp = await _db.OtpCodes.IgnoreQueryFilters().FirstOrDefaultAsync();
        otp.Should().NotBeNull();
        otp!.Email.Should().Be("user@test.com");
        otp.Purpose.Should().Be(OtpPurpose.PasswordReset);
        otp.IsUsed.Should().BeFalse();
        otp.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task SendForgotPasswordOtpAsync_ExistingUnusedOtp_InvalidatesOldAndCreatesNew()
    {
        _db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = "user@test.com",
            Code      = "111111",
            Purpose   = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

        await _sut.SendForgotPasswordOtpAsync("user@test.com");

        var otps = await _db.OtpCodes.IgnoreQueryFilters().ToListAsync();
        otps.Should().HaveCount(2);
        otps.Where(o => o.Code == "111111").Should().AllSatisfy(o => o.IsUsed.Should().BeTrue());
    }

    [Fact]
    public async Task VerifyOtpAsync_ValidCode_ReturnsSuccess()
    {
        _db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = "user@test.com",
            Code      = "123456",
            Purpose   = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        var result = await _sut.VerifyOtpAsync("user@test.com", "123456", "PasswordReset");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidCode_ReturnsFailure()
    {
        var result = await _sut.VerifyOtpAsync("user@test.com", "000000", "PasswordReset");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OTP.Invalid");
    }

    [Fact]
    public async Task VerifyOtpAsync_ExpiredCode_ReturnsFailure()
    {
        _db.OtpCodes.Add(new OtpCode
        {
            Id        = Guid.NewGuid(),
            Email     = "user@test.com",
            Code      = "654321",
            Purpose   = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            IsUsed    = false
        });
        await _db.SaveChangesAsync();

        var result = await _sut.VerifyOtpAsync("user@test.com", "654321", "PasswordReset");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_OTP_EXPIRED");
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidPurpose_ReturnsFailure()
    {
        var result = await _sut.VerifyOtpAsync("user@test.com", "123456", "InvalidPurpose");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OTP.InvalidPurpose");
    }

    [Fact]
    public async Task SendForgotPasswordOtpAsync_UserExists_CallsDeliveryChannel()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@test.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

        await _sut.SendForgotPasswordOtpAsync("user@test.com");

        _deliveryChannelMock.Verify(
            c => c.DeliverAsync("user@test.com", It.IsAny<string>(), "PasswordReset", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendForgotPasswordOtpAsync_UserNotFound_DoesNotCallDeliveryChannel()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("nobody@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        await _sut.SendForgotPasswordOtpAsync("nobody@test.com");

        _deliveryChannelMock.Verify(
            c => c.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
