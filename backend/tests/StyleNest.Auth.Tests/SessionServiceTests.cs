/**
 * ENH-AUTH-004 — Multi-device Session Management (FR-AUTH-008)
 * Acceptance criteria:
 *   - GET sessions returns only active (non-revoked, non-expired) tokens for the user
 *   - IsCurrent flag set when token matches currentToken header
 *   - DELETE session revokes only the target session; others remain active
 *   - DELETE session for another user's sessionId returns false
 *   - RevokeAll revokes all tokens except the supplied current token
 *   - Revoked tokens excluded from subsequent GET sessions
 */

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StyleNest.Auth.API.Services;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using Xunit;

namespace StyleNest.Auth.Tests;

public sealed class SessionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new AppDbContext(options);
        _sut = new SessionService(_db, NullLogger<SessionService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── GetSessionsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSessions_ReturnsOnlyActiveTokensForUser()
    {
        var userId = Guid.NewGuid();
        await SeedTokensAsync(userId,
            ("tok-active-1", revoked: false, expired: false),
            ("tok-revoked",  revoked: true,  expired: false),
            ("tok-expired",  revoked: false, expired: true));

        var sessions = await _sut.GetSessionsAsync(userId, null);

        sessions.Should().HaveCount(1);
        sessions[0].SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSessions_DoesNotReturnOtherUsersTokens()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SeedTokensAsync(userA, ("tok-a", false, false));
        await SeedTokensAsync(userB, ("tok-b", false, false));

        var sessions = await _sut.GetSessionsAsync(userA, null);

        sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSessions_IsCurrent_TrueWhenTokenMatches()
    {
        var userId = Guid.NewGuid();
        await SeedTokensAsync(userId,
            ("tok-A", false, false),
            ("tok-B", false, false));

        var sessions = await _sut.GetSessionsAsync(userId, "tok-A");

        sessions.Should().ContainSingle(s => s.IsCurrent).Which.IsCurrent.Should().BeTrue();
        sessions.Should().ContainSingle(s => !s.IsCurrent).Which.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessions_EmptyWhenNoActiveTokens()
    {
        var userId = Guid.NewGuid();
        await SeedTokensAsync(userId, ("tok-revoked", revoked: true, expired: false));

        var sessions = await _sut.GetSessionsAsync(userId, null);

        sessions.Should().BeEmpty();
    }

    // ── RevokeSessionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeSession_ValidSessionId_RevokesTargetOnly()
    {
        var userId = Guid.NewGuid();
        var ids = await SeedTokensAsync(userId,
            ("tok-1", false, false),
            ("tok-2", false, false));

        var result = await _sut.RevokeSessionAsync(userId, ids[0]);

        result.Should().BeTrue();
        var remaining = await _sut.GetSessionsAsync(userId, null);
        remaining.Should().HaveCount(1);
        remaining[0].SessionId.Should().Be(ids[1]);
    }

    [Fact]
    public async Task RevokeSession_WrongUserId_ReturnsFalse()
    {
        var userA   = Guid.NewGuid();
        var userB   = Guid.NewGuid();
        var ids = await SeedTokensAsync(userA, ("tok-a", false, false));

        var result = await _sut.RevokeSessionAsync(userB, ids[0]);

        result.Should().BeFalse();
        // userA session must still be active
        var sessions = await _sut.GetSessionsAsync(userA, null);
        sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task RevokeSession_NonExistentSessionId_ReturnsFalse()
    {
        var result = await _sut.RevokeSessionAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── RevokeAllSessionsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAllSessions_RevokesAllExceptCurrentToken()
    {
        var userId = Guid.NewGuid();
        await SeedTokensAsync(userId,
            ("tok-1", false, false),
            ("tok-2", false, false),
            ("tok-3", false, false));

        await _sut.RevokeAllSessionsAsync(userId, "tok-2");

        var remaining = await _sut.GetSessionsAsync(userId, null);
        remaining.Should().HaveCount(1);
        remaining[0].SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RevokeAllSessions_NullCurrentToken_RevokesAll()
    {
        var userId = Guid.NewGuid();
        await SeedTokensAsync(userId,
            ("tok-1", false, false),
            ("tok-2", false, false));

        await _sut.RevokeAllSessionsAsync(userId, null);

        var remaining = await _sut.GetSessionsAsync(userId, null);
        remaining.Should().BeEmpty();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<List<Guid>> SeedTokensAsync(
        Guid userId,
        params (string Token, bool Revoked, bool Expired)[] specs)
    {
        var ids = new List<Guid>();
        foreach (var (token, revoked, expired) in specs)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            _db.RefreshTokens.Add(new RefreshToken
            {
                Id        = id,
                UserId    = userId,
                Token     = token,
                IsRevoked = revoked,
                ExpiresAt = expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
            });
        }
        await _db.SaveChangesAsync();
        return ids;
    }
}
