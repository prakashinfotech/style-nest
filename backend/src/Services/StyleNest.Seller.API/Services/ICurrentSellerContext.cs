using System.Security.Claims;

namespace StyleNest.Seller.API.Services;

public interface ICurrentSellerContext
{
    /// <summary>Seller ID from the authenticated JWT, or null if not a seller request.</summary>
    Guid? SellerId { get; }
}

/// <summary>
/// ENH-SELL-001 — Reads the seller_id claim injected by Auth.API when issuing tokens for Seller-role users.
/// Null when the caller is an Admin, SuperAdmin, Customer, or unauthenticated.
/// </summary>
public sealed class HttpCurrentSellerContext(IHttpContextAccessor httpContextAccessor) : ICurrentSellerContext
{
    private const string SellerIdClaim = "seller_id";

    public Guid? SellerId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(SellerIdClaim);
            return value is not null && Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
