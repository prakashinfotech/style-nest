/**
 * ENH-AUTH-011 — PDPB / GDPR Right-to-Erasure (BR-SEC-003)
 * Acceptance criteria:
 *   - User not found → Result.Failure(NotFound)
 *   - PII anonymised: Email=null, NormalizedEmail=null, PhoneNumber=null, AvatarUrl=null,
 *     FirstName="Deleted", LastName="User", PasswordHash=null, UserName=deleted_{suffix}
 *   - Account permanently locked (LockoutEnd=DateTimeOffset.MaxValue)
 *   - All active RefreshTokens revoked (IsRevoked=true)
 *   - Cart + CartItems soft-deleted (IsDeleted=true)
 *   - Wishlist + WishlistItems soft-deleted (IsDeleted=true)
 *   - Returns Result.Success()
 */

using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.Infrastructure.Entities.Commerce;
using StyleNest.Infrastructure.Persistence;
using StyleNest.User.API.Services;
using Xunit;

namespace StyleNest.User.Tests;

public sealed class ErasureServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ErasureService _sut;

    public ErasureServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(opts);

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _sut = new ErasureService(_db, _userManagerMock.Object, NullLogger<ErasureService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── user not found ─────────────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.EraseUserAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    // ── PII anonymisation ──────────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_AnonymisesPii_EmailAndPhoneNull()
    {
        var (user, _) = await ArrangeUserAsync();

        var result = await _sut.EraseUserAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        user.Email.Should().BeNull();
        user.NormalizedEmail.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task EraseUserAsync_AnonymisesPii_NamesSetToDeleted()
    {
        var (user, _) = await ArrangeUserAsync();

        await _sut.EraseUserAsync(user.Id);

        user.FirstName.Should().Be("Deleted");
        user.LastName.Should().Be("User");
    }

    [Fact]
    public async Task EraseUserAsync_AnonymisesPii_UserNameContainsDeletedPrefix()
    {
        var (user, _) = await ArrangeUserAsync();

        await _sut.EraseUserAsync(user.Id);

        user.UserName.Should().StartWith("deleted_");
    }

    [Fact]
    public async Task EraseUserAsync_AnonymisesPii_PasswordHashSetToNull()
    {
        var (user, _) = await ArrangeUserAsync();
        user.PasswordHash = "some-bcrypt-hash";

        await _sut.EraseUserAsync(user.Id);

        user.PasswordHash.Should().BeNull();
    }

    // ── account lock ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_PermanentlyLocksAccount()
    {
        var (user, _) = await ArrangeUserAsync();

        await _sut.EraseUserAsync(user.Id);

        user.LockoutEnabled.Should().BeTrue();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
    }

    // ── session revocation ─────────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_RevokesAllActiveRefreshTokens()
    {
        var (user, _) = await ArrangeUserAsync();
        var tokenId = await SeedRefreshTokenAsync(user.Id, revoked: false);

        await _sut.EraseUserAsync(user.Id);

        var token = await _db.RefreshTokens.FindAsync(tokenId);
        token!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task EraseUserAsync_DoesNotTouchAlreadyRevokedTokens()
    {
        var (user, _) = await ArrangeUserAsync();
        await SeedRefreshTokenAsync(user.Id, revoked: true);

        var result = await _sut.EraseUserAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
    }

    // ── cart soft-delete ───────────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_SoftDeletesCartAndCartItems()
    {
        var (user, _) = await ArrangeUserAsync();
        var (cartId, itemId) = await SeedCartAsync(user.Id);

        await _sut.EraseUserAsync(user.Id);

        var cart = await _db.Carts.IgnoreQueryFilters().FirstAsync(c => c.Id == cartId);
        var item = await _db.CartItems.IgnoreQueryFilters().FirstAsync(ci => ci.Id == itemId);
        cart.IsDeleted.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
    }

    // ── wishlist soft-delete ───────────────────────────────────────────────────

    [Fact]
    public async Task EraseUserAsync_SoftDeletesWishlistAndWishlistItems()
    {
        var (user, _) = await ArrangeUserAsync();
        var (wishlistId, itemId) = await SeedWishlistAsync(user.Id);

        await _sut.EraseUserAsync(user.Id);

        var wishlist = await _db.Wishlists.IgnoreQueryFilters().FirstAsync(w => w.Id == wishlistId);
        var item = await _db.WishlistItems.IgnoreQueryFilters().FirstAsync(wi => wi.Id == itemId);
        wishlist.IsDeleted.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(ApplicationUser user, string updateStamp)> ArrangeUserAsync()
    {
        var user = new ApplicationUser
        {
            Id          = Guid.NewGuid(),
            Email       = "victim@test.com",
            NormalizedEmail = "VICTIM@TEST.COM",
            UserName    = "victim@test.com",
            NormalizedUserName = "VICTIM@TEST.COM",
            FirstName   = "Victim",
            LastName    = "User",
            PhoneNumber = "+919876543210",
            PasswordHash = "hash",
            AvatarUrl   = "https://cdn.example.com/avatar.jpg",
        };

        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        return (user, user.SecurityStamp ?? "");
    }

    private async Task<Guid> SeedRefreshTokenAsync(Guid userId, bool revoked)
    {
        var token = new StyleNest.Infrastructure.Entities.Auth.RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = revoked,
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token.Id;
    }

    private async Task<(Guid cartId, Guid itemId)> SeedCartAsync(Guid userId)
    {
        var cart = new Cart
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            IsDeleted = false,
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();

        var item = new CartItem
        {
            Id               = Guid.NewGuid(),
            CartId           = cart.Id,
            ProductVariantId = Guid.NewGuid(),
            Quantity         = 1,
        };
        _db.CartItems.Add(item);
        await _db.SaveChangesAsync();

        return (cart.Id, item.Id);
    }

    private async Task<(Guid wishlistId, Guid itemId)> SeedWishlistAsync(Guid userId)
    {
        var wishlist = new Wishlist
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            IsDeleted = false,
        };
        _db.Wishlists.Add(wishlist);
        await _db.SaveChangesAsync();

        var item = new WishlistItem
        {
            Id         = Guid.NewGuid(),
            WishlistId = wishlist.Id,
            ProductId  = Guid.NewGuid(),
        };
        _db.WishlistItems.Add(item);
        await _db.SaveChangesAsync();

        return (wishlist.Id, item.Id);
    }
}
