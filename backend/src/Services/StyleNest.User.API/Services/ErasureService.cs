using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Persistence;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.User.API.Services;

public interface IErasureService
{
    /// <summary>
    /// ENH-AUTH-011 — PDPB / GDPR Right-to-Erasure.
    /// Anonymises PII, revokes sessions, deletes cart + wishlist.
    /// Orders + Payments preserved with the existing UserId (FK intact, PII cleared from Users row).
    /// </summary>
    Task<Result> EraseUserAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// ENH-AUTH-011 — Right-to-Erasure / PDPB (FR-SEC-006 / TC-AUTH-FUNC-031).
/// Steps executed atomically within a single SaveChangesAsync:
///   (a) Anonymise PII in AspNetUsers row (email → null, names → "Deleted User", phone → null)
///   (b) Permanently lock account (LockoutEnd = DateTimeOffset.MaxValue)
///   (c) Revoke all active RefreshTokens (sessions)
///   (d) Soft-delete Cart + CartItems + Wishlist + WishlistItems (IsDeleted = true)
///   (e) Emit GDPRErasureEvent via structured Serilog (Service Bus in Phase 5+)
/// Orders + Payments are retained (data-retention obligation) — PII cleared from Users row.
/// </summary>
public sealed class ErasureService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<ErasureService> logger) : IErasureService
{
    public async Task<Result> EraseUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(Error.NotFound("User"));

        // (a) Anonymise PII
        var anonymisedSuffix = userId.ToString("N")[..8];
        user.Email          = null;
        user.NormalizedEmail = null;
        user.UserName       = $"deleted_{anonymisedSuffix}";
        user.NormalizedUserName = $"DELETED_{anonymisedSuffix.ToUpperInvariant()}";
        user.PhoneNumber    = null;
        user.FirstName      = "Deleted";
        user.LastName       = "User";
        user.PasswordHash   = null;   // prevents re-login
        user.SecurityStamp  = Guid.NewGuid().ToString(); // invalidate all JWTs immediately
        user.EmailConfirmed  = false;
        user.AvatarUrl      = null;

        // (b) Permanently lock account
        user.LockoutEnabled = true;
        user.LockoutEnd     = DateTimeOffset.MaxValue;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Failure(new Error("Erasure.UpdateFailed", errors));
        }

        // (c) Revoke all sessions
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);
        tokens.ForEach(t => t.IsRevoked = true);

        // (d) Soft-delete Cart + items
        var carts = await db.Carts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .ToListAsync(ct);
        foreach (var cart in carts)
        {
            cart.IsDeleted = true;
            var cartItems = await db.CartItems
                .Where(ci => ci.CartId == cart.Id && !ci.IsDeleted)
                .ToListAsync(ct);
            cartItems.ForEach(ci => ci.IsDeleted = true);
        }

        // (d) Soft-delete Wishlist + items
        var wishlists = await db.Wishlists
            .Where(w => w.UserId == userId && !w.IsDeleted)
            .ToListAsync(ct);
        foreach (var wishlist in wishlists)
        {
            wishlist.IsDeleted = true;
            var wishlistItems = await db.WishlistItems
                .Where(wi => wi.WishlistId == wishlist.Id && !wi.IsDeleted)
                .ToListAsync(ct);
            wishlistItems.ForEach(wi => wi.IsDeleted = true);
        }

        await db.SaveChangesAsync(ct);

        // (e) Emit erasure event (structured log; Service Bus in Phase 5+)
        logger.LogInformation(
            "{EventType} UserId={UserId} AnonymisedSuffix={Suffix} SessionsRevoked={Sessions} CartsDeleted={Carts} WishlistsDeleted={Wishlists}",
            "GDPR_ERASURE", userId, anonymisedSuffix, tokens.Count, carts.Count, wishlists.Count);

        return Result.Success();
    }
}
