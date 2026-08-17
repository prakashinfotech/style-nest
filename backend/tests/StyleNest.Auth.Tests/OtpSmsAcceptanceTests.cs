// ENH-AUTH-009 Acceptance Test Suite
// Validates: FR-AUTH-001, BR-AUTH-001, EC-AUTH-001, EC-AUTH-002, EC-AUTH-012
// All 5 tests should PASS after the ENH-AUTH-009 implementation is complete.

using System.Reflection;
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

public sealed class OtpSmsAcceptanceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpDeliveryChannel> _deliveryMock;
    private readonly Mock<ISmsDeliveryChannel> _smsMock;
    private readonly OtpService _sut;

    public OtpSmsAcceptanceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _deliveryMock = new Mock<IOtpDeliveryChannel>();
        _deliveryMock
            .Setup(c => c.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smsMock = new Mock<ISmsDeliveryChannel>();
        _smsMock
            .Setup(c => c.DeliverAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        _sut = new OtpService(_db, _userManagerMock.Object, _deliveryMock.Object, _smsMock.Object, NullLogger<OtpService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── AC1 — OTP is exactly 6 numeric digits [BR-AUTH-001] ─────────────────
    [Fact]
    public void GenerateCode_ProducesSixDigitNumericString_Always()
    {
        var method = typeof(OtpService).GetMethod(
            "GenerateCode",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("private static GenerateCode() must exist on OtpService");

        for (var i = 0; i < 100; i++)
        {
            var code = (string)method!.Invoke(null, null)!;
            code.Should().MatchRegex(@"^\d{6}$",
                $"iteration {i}: OTP must be exactly 6 numeric digits (BR-AUTH-001)");
        }
    }

    // ── AC2 — Single-use: second verify must fail [BR-AUTH-001] ─────────────
    [Fact]
    public async Task VerifyPhoneOtp_AfterSuccessfulVerify_SecondCallMustFail()
    {
        var result1 = await _sut.SendPhoneOtpAsync("+919876543210");
        result1.IsSuccess.Should().BeTrue("send must succeed");

        var code = (await _db.OtpCodes.IgnoreQueryFilters()
            .FirstAsync(o => o.PhoneNumber == "+919876543210")).Code;

        var first = await _sut.VerifyPhoneOtpAsync("+919876543210", code);
        first.IsSuccess.Should().BeTrue("first verify must succeed");

        // Single-use enforcement: second attempt must fail (BR-AUTH-001)
        var second = await _sut.VerifyPhoneOtpAsync("+919876543210", code);
        second.IsFailure.Should().BeTrue("second verify must fail — OTP is single-use per BR-AUTH-001");
    }

    // ── AC3 — Expired OTP → error code AUTH_OTP_EXPIRED [EC-AUTH-002] ───────
    [Fact]
    public async Task VerifyPhoneOtp_ExpiredOtp_ReturnsAuthOtpExpiredErrorCode()
    {
        _db.OtpCodes.Add(new OtpCode
        {
            Id          = Guid.NewGuid(),
            PhoneNumber = "+919876543211",
            Email       = string.Empty,
            Code        = "999888",
            Purpose     = OtpPurpose.PhoneVerification,
            ExpiresAt   = DateTime.UtcNow.AddSeconds(-1),
            IsUsed      = false
        });
        await _db.SaveChangesAsync();

        var result = await _sut.VerifyPhoneOtpAsync("+919876543211", "999888");

        result.IsFailure.Should().BeTrue("expired OTP must return failure");
        result.Error.Code.Should().Be("AUTH_OTP_EXPIRED",
            "EC-AUTH-002: expired OTP must return AUTH_OTP_EXPIRED");
    }

    // ── AC4 — OTP expiry = 300s [FR-AUTH-001] ───────────────────────────────
    [Fact]
    public async Task SendPhoneOtp_OtpExpiryIs300Seconds()
    {
        var before = DateTime.UtcNow;
        var result = await _sut.SendPhoneOtpAsync("+919876543212");
        result.IsSuccess.Should().BeTrue();

        var otp = await _db.OtpCodes.IgnoreQueryFilters()
            .FirstAsync(o => o.PhoneNumber == "+919876543212");

        // FR-AUTH-001: expiry = now + 300s (±10s tolerance for execution time)
        otp.ExpiresAt.Should()
            .BeAfter(before.AddSeconds(290))
            .And.BeBefore(before.AddSeconds(310),
                "FR-AUTH-001 requires OTP expiry = now + 300s");

        // Response also carries the correct masked phone
        result.Value.MaskedPhone.Should().MatchRegex(@"^\+91-XXX-XXX-\d{4}$",
            "FR-AUTH-001: masked phone must match +91-XXX-XXX-NNNN");
    }

    // ── AC5 — 6th OTP within 1h → HTTP 429 equivalent [EC-AUTH-012] ─────────
    [Fact]
    public async Task SendPhoneOtp_SixthRequestWithinOneHour_ReturnsRateLimitFailure()
    {
        const string phone = "+919876543213";

        // Requests 1–5 must succeed (EC-AUTH-012 allows 5/hour)
        for (var i = 1; i <= 5; i++)
        {
            var r = await _sut.SendPhoneOtpAsync(phone);
            r.IsSuccess.Should().BeTrue($"OTP request {i} of 5 within 1h must succeed");
        }

        // 6th within same hour must fail
        var sixth = await _sut.SendPhoneOtpAsync(phone);
        sixth.IsFailure.Should().BeTrue("6th OTP within 1h must fail per EC-AUTH-012");
        sixth.Error.Code.Should().Be("OTP.RateLimitExceeded",
            "6th request must return OTP.RateLimitExceeded");
    }
}
