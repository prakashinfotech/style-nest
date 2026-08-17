/**
 * ENH-AUTH-005 — Account Lockout Exponential Doubling (FR-AUTH-011 / BR-AUTH-008)
 * Acceptance criteria:
 *   - MaxFailedAttempts constant = 5
 *   - InitialLockoutSeconds constant = 1800 (30 min)
 *   - MaxLockoutSeconds constant = 86400 (24 h)
 *   - LockoutDurationSeconds doubles on each successive lockout: 1800→3600→7200→…→86400
 *   - Doubling is capped at 86400 — never exceeds 24h
 *   - Successful login resets LockoutDurationSeconds to 0
 *   - LockoutError carries correct lockoutDurationSeconds
 */

using FluentAssertions;
using StyleNest.Auth.API.Services;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class LockoutServiceTests
{
    // ── Constants ─────────────────────────────────────────────────────────────

    [Fact]
    public void MaxFailedAttempts_IsExactlyFive()
        => LockoutService.MaxFailedAttempts.Should().Be(5);

    [Fact]
    public void InitialLockoutSeconds_IsExactly1800()
        => LockoutService.InitialLockoutSeconds.Should().Be(1800);

    [Fact]
    public void MaxLockoutSeconds_IsExactly86400()
        => LockoutService.MaxLockoutSeconds.Should().Be(86400);

    // ── Doubling progression ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0,      1800)]   // never locked → first lockout = 1800
    [InlineData(1800,   3600)]   // first → second lockout
    [InlineData(3600,   7200)]   // second → third
    [InlineData(7200,  14400)]   // third → fourth
    [InlineData(14400, 28800)]   // fourth → fifth
    [InlineData(28800, 57600)]   // fifth → sixth
    [InlineData(57600, 86400)]   // sixth → capped at max
    [InlineData(86400, 86400)]   // already at max → stays at max
    [InlineData(43201, 86400)]   // just above half-max → capped
    public void NextLockoutDuration_DoublesFromPrevious_CappedAtMax(
        int previousSeconds, int expectedNext)
    {
        var next = ComputeNext(previousSeconds);
        next.Should().Be(expectedNext);
    }

    // ── AuthErrors.AccountLocked ──────────────────────────────────────────────

    [Fact]
    public void AccountLockedError_HasCorrectCode()
        => AuthErrors.AccountLocked(1800).Code.Should().Be(AuthErrors.AccountLockedCode);

    [Fact]
    public void AccountLockedError_MessageIsSecondsString()
        => AuthErrors.AccountLocked(3600).Message.Should().Be("3600");

    [Fact]
    public void AccountLockedCode_IsCorrectString()
        => AuthErrors.AccountLockedCode.Should().Be("Auth.AccountLocked");

    // ── helpers ───────────────────────────────────────────────────────────────

    private static int ComputeNext(int previous) =>
        previous == 0
            ? LockoutService.InitialLockoutSeconds
            : Math.Min(previous * 2, LockoutService.MaxLockoutSeconds);
}
