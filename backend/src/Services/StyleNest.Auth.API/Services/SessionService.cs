using Microsoft.EntityFrameworkCore;
using StyleNest.Auth.API.DTOs;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Auth.API.Services;

public interface ISessionService
{
    Task<IReadOnlyList<SessionDto>> GetSessionsAsync(Guid userId, string? currentToken, CancellationToken ct = default);
    Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task RevokeAllSessionsAsync(Guid userId, string? exceptToken, CancellationToken ct = default);
}

/// <summary>
/// ENH-AUTH-004 — Multi-device session management backed by RefreshTokens table.
/// Each active (non-revoked, non-expired) RefreshToken represents one device session.
/// </summary>
public sealed class SessionService(AppDbContext db, ILogger<SessionService> logger) : ISessionService
{
    public async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(
        Guid userId, string? currentToken, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var sessions = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SessionDto(
                t.Id,
                t.DeviceName,
                t.IpAddress,
                t.CreatedAt,
                t.ExpiresAt,
                t.Token == currentToken))
            .ToListAsync(ct);

        return sessions;
    }

    public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, ct);

        if (token is null)
            return false;

        token.IsRevoked = true;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Session {SessionId} revoked for user {UserId}", sessionId, userId);

        return true;
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string? exceptToken, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.Token != exceptToken)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.IsRevoked = true;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "All sessions revoked for user {UserId} (except current)", userId);
    }
}
