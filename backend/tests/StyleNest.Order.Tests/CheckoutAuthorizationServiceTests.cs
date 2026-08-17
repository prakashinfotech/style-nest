/**
 * ENH-CHKOUT-001 — Email Verification Gate for Checkout > ₹5,000
 * BR-AUTH-003: unverified email + order > ₹5,000 → CHECKOUT_EMAIL_UNVERIFIED (HTTP 403)
 * Boundary: exactly ₹5,000 → allowed; ₹5,001 → blocked.
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Exceptions;
using StyleNest.Order.API.Services;
using Xunit;

namespace StyleNest.Order.Tests;

public sealed class CheckoutAuthorizationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CheckoutAuthorizationService _sut;

    public CheckoutAuthorizationServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(opts);
        _sut = new CheckoutAuthorizationService(_db, NullLogger<CheckoutAuthorizationService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private async Task<ApplicationUser> SeedUserAsync(bool emailConfirmed, string email = "test@example.com")
    {
        var user = new ApplicationUser
        {
            Id                  = Guid.NewGuid(),
            Email               = email,
            UserName            = email,
            NormalizedEmail     = email.ToUpperInvariant(),
            NormalizedUserName  = email.ToUpperInvariant(),
            SecurityStamp       = Guid.NewGuid().ToString(),
            EmailConfirmed      = emailConfirmed,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    // ── BR-AUTH-003: unverified + above threshold → throws ───────────────────

    [Fact]
    public async Task ValidateEmailAsync_UnverifiedEmail_OrderAbove5000_ThrowsCheckoutEmailUnverifiedException()
    {
        var user = await SeedUserAsync(emailConfirmed: false);

        var act = () => _sut.ValidateEmailAsync(user.Id, 5_001m);

        await act.Should().ThrowAsync<CheckoutEmailUnverifiedException>();
    }

    // ── Boundary: exactly ₹5,000 → allowed (> not >=) ────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_UnverifiedEmail_OrderExactly5000_Passes()
    {
        var user = await SeedUserAsync(emailConfirmed: false);

        var act = () => _sut.ValidateEmailAsync(user.Id, 5_000m);

        await act.Should().NotThrowAsync();
    }

    // ── Verified email + any amount → always passes ───────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_VerifiedEmail_OrderAbove5000_Passes()
    {
        var user = await SeedUserAsync(emailConfirmed: true);

        var act = () => _sut.ValidateEmailAsync(user.Id, 10_000m);

        await act.Should().NotThrowAsync();
    }

    // ── Unverified email + below threshold → passes ───────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_UnverifiedEmail_OrderBelow5000_Passes()
    {
        var user = await SeedUserAsync(emailConfirmed: false);

        var act = () => _sut.ValidateEmailAsync(user.Id, 4_999m);

        await act.Should().NotThrowAsync();
    }

    // ── Threshold constant is exactly ₹5,000 ─────────────────────────────────

    [Fact]
    public void EmailVerificationThreshold_IsExactly5000()
    {
        CheckoutAuthorizationService.EmailVerificationThreshold.Should().Be(5_000m);
    }

    // ── Exception carries the user's email ───────────────────────────────────

    [Fact]
    public async Task ValidateEmailAsync_ExceptionContainsEmail()
    {
        var user = await SeedUserAsync(emailConfirmed: false, email: "buyer@example.com");

        var ex = await Assert.ThrowsAsync<CheckoutEmailUnverifiedException>(
            () => _sut.ValidateEmailAsync(user.Id, 6_000m));

        ex.Email.Should().Be("buyer@example.com");
        ex.Message.Should().Contain("buyer@example.com");
    }

    // ── ₹5,001 is strictly blocked, ₹4,999 is strictly allowed ──────────────

    [Theory]
    [InlineData(5_001, true,  true)]   // unverified, 5001 → blocked
    [InlineData(5_001, false, false)]  // verified,   5001 → allowed
    [InlineData(4_999, true,  false)]  // unverified, 4999 → allowed
    [InlineData(5_000, true,  false)]  // unverified, exactly 5000 → allowed
    public async Task ValidateEmailAsync_BoundaryMatrix(decimal total, bool emailUnverified, bool shouldThrow)
    {
        var user = await SeedUserAsync(emailConfirmed: !emailUnverified);

        var act = () => _sut.ValidateEmailAsync(user.Id, total);

        if (shouldThrow)
            await act.Should().ThrowAsync<CheckoutEmailUnverifiedException>();
        else
            await act.Should().NotThrowAsync();
    }
}
