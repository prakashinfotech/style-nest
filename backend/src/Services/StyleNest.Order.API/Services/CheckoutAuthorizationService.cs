using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Persistence;
using StyleNest.Order.API.Exceptions;

namespace StyleNest.Order.API.Services;

public interface ICheckoutAuthorizationService
{
    /// <summary>
    /// Validates that the user may proceed with checkout at the given order total.
    /// Throws <see cref="CheckoutEmailUnverifiedException"/> when:
    ///   orderTotal &gt; ₹5,000 AND user.EmailConfirmed == false (BR-AUTH-003).
    /// </summary>
    Task ValidateEmailAsync(Guid userId, decimal orderTotal, CancellationToken ct = default);
}

public sealed class CheckoutAuthorizationService(
    AppDbContext db,
    ILogger<CheckoutAuthorizationService> logger) : ICheckoutAuthorizationService
{
    // ENH-CHKOUT-001: BR-AUTH-003 — strictly greater than ₹5,000 triggers gate
    public const decimal EmailVerificationThreshold = 5_000m;

    public async Task ValidateEmailAsync(Guid userId, decimal orderTotal, CancellationToken ct = default)
    {
        if (orderTotal <= EmailVerificationThreshold)
            return;

        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.EmailConfirmed })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"User {userId} not found during checkout authorization.");

        if (!user.EmailConfirmed)
        {
            logger.LogWarning("Checkout blocked: unverified email for user {UserId}, order total {Total:C}", userId, orderTotal);
            throw new CheckoutEmailUnverifiedException(user.Email ?? string.Empty);
        }
    }
}
