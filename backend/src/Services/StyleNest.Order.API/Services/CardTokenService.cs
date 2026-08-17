/**
 * ENH-PAY-006 — Razorpay Vault Tokenisation
 * Acceptance criteria:
 *   - SaveTokenAsync: persists token_id + last4 + network; never stores PAN or CVV
 *   - SaveTokenAsync: duplicate (userId, razorpayTokenId) → throws InvalidOperationException
 *   - SaveTokenAsync: first token for user is automatically set as default
 *   - GetUserTokensAsync: returns only the requesting user's tokens (cross-tenant isolation)
 *   - GetTokenAsync: returns null for unknown id or wrong user
 *   - DeleteTokenAsync: soft-deletes; throws KeyNotFoundException for wrong user/unknown id
 *   - DeleteTokenAsync: if deleted token was default, promotes the most-recently-added remaining token
 *   - SetDefaultAsync: clears all other IsDefault flags then sets the target; throws for wrong user
 *   - All methods accept CancellationToken
 */

using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Payments;
using StyleNest.Infrastructure.Persistence;

namespace StyleNest.Order.API.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Payload sent by the Angular checkout when Razorpay returns a vault token.</summary>
public sealed record SaveCardTokenRequest(
    string     RazorpayTokenId,
    string?    RazorpayCustomerId,
    string     Last4,
    CardNetwork Network,
    int        ExpiryMonth,
    int        ExpiryYear,
    string?    CardholderName);

/// <summary>Response DTO — safe card metadata for UI display.</summary>
public sealed record CardTokenDto(
    Guid        Id,
    string      RazorpayTokenId,
    string?     RazorpayCustomerId,
    string      Last4,
    CardNetwork Network,
    string      NetworkDisplay,
    int         ExpiryMonth,
    int         ExpiryYear,
    string?     CardholderName,
    bool        IsDefault,
    bool        IsExpired,
    DateTime    CreatedAt);

// ── Abstraction ───────────────────────────────────────────────────────────────

public interface ICardTokenService
{
    /// <summary>
    /// Saves a Razorpay vault token reference for a user.
    /// The first token for a user is automatically set as default.
    /// Throws <see cref="InvalidOperationException"/> when the token already exists for this user.
    /// </summary>
    Task<CardTokenDto> SaveTokenAsync(
        Guid userId, SaveCardTokenRequest request, CancellationToken ct = default);

    /// <summary>Returns all non-deleted card tokens for the user, newest first.</summary>
    Task<IEnumerable<CardTokenDto>> GetUserTokensAsync(
        Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a specific token only if it belongs to <paramref name="userId"/>.
    /// Returns <see langword="null"/> when not found or owned by a different user.
    /// </summary>
    Task<CardTokenDto?> GetTokenAsync(
        Guid userId, Guid tokenId, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a token. Throws <see cref="KeyNotFoundException"/> when the token does not
    /// exist or belongs to a different user. If the deleted token was the default, the
    /// most-recently-created remaining token is automatically promoted.
    /// </summary>
    Task DeleteTokenAsync(Guid userId, Guid tokenId, CancellationToken ct = default);

    /// <summary>
    /// Sets the specified token as the user's default card.
    /// All other tokens for the user have <c>IsDefault</c> cleared atomically.
    /// Throws <see cref="KeyNotFoundException"/> when the token does not exist or belongs
    /// to a different user.
    /// </summary>
    Task SetDefaultAsync(Guid userId, Guid tokenId, CancellationToken ct = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class CardTokenService(AppDbContext db) : ICardTokenService
{
    public async Task<CardTokenDto> SaveTokenAsync(
        Guid userId, SaveCardTokenRequest request, CancellationToken ct = default)
    {
        // Guard: no duplicate token for this user
        var exists = await db.CardTokens
            .AnyAsync(t => t.UserId == userId &&
                           t.RazorpayTokenId == request.RazorpayTokenId, ct);

        if (exists)
            throw new InvalidOperationException(
                $"Card token '{request.RazorpayTokenId}' is already saved for this account.");

        // First card for the user is automatically set as default
        var hasAny = await db.CardTokens.AnyAsync(t => t.UserId == userId, ct);

        var token = new CardToken
        {
            Id                 = Guid.NewGuid(),
            UserId             = userId,
            RazorpayTokenId    = request.RazorpayTokenId,
            RazorpayCustomerId = request.RazorpayCustomerId,
            Last4              = request.Last4,
            Network            = request.Network,
            ExpiryMonth        = request.ExpiryMonth,
            ExpiryYear         = request.ExpiryYear,
            CardholderName     = request.CardholderName,
            IsDefault          = !hasAny, // first card → auto-default
        };

        db.CardTokens.Add(token);
        await db.SaveChangesAsync(ct);

        return Map(token);
    }

    public async Task<IEnumerable<CardTokenDto>> GetUserTokensAsync(
        Guid userId, CancellationToken ct = default)
    {
        var tokens = await db.CardTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tokens.Select(Map);
    }

    public async Task<CardTokenDto?> GetTokenAsync(
        Guid userId, Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.CardTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, ct);

        return token is null ? null : Map(token);
    }

    public async Task DeleteTokenAsync(
        Guid userId, Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.CardTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException(
                $"Card token {tokenId} not found or not owned by the requesting user.");

        var wasDefault = token.IsDefault;
        token.IsDeleted = true;
        token.IsDefault = false;

        // Promote the most-recently-created remaining token to default
        if (wasDefault)
        {
            var successor = await db.CardTokens
                .Where(t => t.UserId == userId && t.Id != tokenId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (successor is not null)
                successor.IsDefault = true;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task SetDefaultAsync(
        Guid userId, Guid tokenId, CancellationToken ct = default)
    {
        var target = await db.CardTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException(
                $"Card token {tokenId} not found or not owned by the requesting user.");

        // Clear all existing defaults for this user, then set the target
        var existing = await db.CardTokens
            .Where(t => t.UserId == userId && t.IsDefault)
            .ToListAsync(ct);

        foreach (var t in existing)
            t.IsDefault = false;

        target.IsDefault = true;
        await db.SaveChangesAsync(ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static CardTokenDto Map(CardToken t)
    {
        var now     = DateTime.UtcNow;
        var expired = t.ExpiryYear < now.Year ||
                      (t.ExpiryYear == now.Year && t.ExpiryMonth < now.Month);

        return new CardTokenDto(
            t.Id,
            t.RazorpayTokenId,
            t.RazorpayCustomerId,
            t.Last4,
            t.Network,
            NetworkDisplay(t.Network),
            t.ExpiryMonth,
            t.ExpiryYear,
            t.CardholderName,
            t.IsDefault,
            IsExpired: expired,
            t.CreatedAt);
    }

    private static string NetworkDisplay(CardNetwork network) => network switch
    {
        CardNetwork.Visa       => "Visa",
        CardNetwork.Mastercard => "Mastercard",
        CardNetwork.Amex       => "American Express",
        CardNetwork.Rupay      => "RuPay",
        CardNetwork.Maestro    => "Maestro",
        CardNetwork.Diners     => "Diners Club",
        _                      => "Unknown",
    };
}
