using Microsoft.EntityFrameworkCore;
using StyleNest.Cart.API.DTOs;
using StyleNest.Cart.API.Exceptions;
using StyleNest.Infrastructure.Entities.Admin;
using StyleNest.Infrastructure.Entities.Orders;
using StyleNest.Infrastructure.Persistence;
using CartEntity     = StyleNest.Infrastructure.Entities.Commerce.Cart;
using CartItemEntity = StyleNest.Infrastructure.Entities.Commerce.CartItem;

namespace StyleNest.Cart.API.Services;

public interface ICartService
{
    Task<CartDto>  GetCartAsync(Guid userId, CancellationToken ct = default);
    Task<CartDto>  AddItemAsync(Guid userId, Guid productId, string? size, string? colour, int quantity, CancellationToken ct = default);
    Task<CartDto>  UpdateItemAsync(Guid userId, Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<CartDto>  RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken ct = default);
    Task<CartDto>  ApplyCouponAsync(Guid userId, string code, CancellationToken ct = default);
}

public sealed class CartService(AppDbContext db) : ICartService
{
    public async Task<CartDto> GetCartAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        return MapCart(cart, null, 0m);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, Guid productId, string? size, string? colour, int quantity, CancellationToken ct = default)
    {
        var variantQuery = db.ProductVariants.Where(v => v.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(size))   variantQuery = variantQuery.Where(v => v.Size == size);
        if (!string.IsNullOrWhiteSpace(colour)) variantQuery = variantQuery.Where(v => v.Colour == colour);

        var variant = await variantQuery.FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Product variant not found.");

        var cart = await GetOrCreateCartAsync(userId, ct);

        var existing = cart.Items.FirstOrDefault(i => i.ProductVariantId == variant.Id);
        if (existing is not null)
        {
            existing.Quantity = Math.Min(existing.Quantity + quantity, 20);
        }
        else
        {
            cart.Items.Add(new CartItemEntity
            {
                CartId           = cart.Id,
                ProductVariantId = variant.Id,
                Quantity         = quantity
            });
        }

        await db.SaveChangesAsync(ct);
        await db.Entry(cart).ReloadAsync(ct);
        cart = await LoadCartAsync(cart.Id, ct);
        return MapCart(cart!, null, 0m);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, int quantity, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        item.Quantity = quantity;
        await db.SaveChangesAsync(ct);
        cart = await LoadCartAsync(cart.Id, ct);
        return MapCart(cart!, null, 0m);
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(ct);
        cart = await LoadCartAsync(cart.Id, ct);
        return MapCart(cart!, null, 0m);
    }

    public async Task<CartDto> ApplyCouponAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var normalizedCode = code.ToUpperInvariant();

        var coupon = await db.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, ct);

        if (coupon is null)
            throw new CouponValidationException("COUPON_NOT_FOUND", "Coupon code not found. Please check and try again.");

        if (!coupon.IsActive)
            throw new CouponValidationException("COUPON_INACTIVE", "This coupon is no longer active.");

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            throw new CouponValidationException("COUPON_EXPIRED", "This coupon has expired.");

        if (coupon.StartsAt.HasValue && coupon.StartsAt.Value > DateTime.UtcNow)
            throw new CouponValidationException("COUPON_NOT_YET_VALID", "This coupon is not yet valid.");

        if (coupon.TotalUsageLimit.HasValue && coupon.UsedCount >= coupon.TotalUsageLimit.Value)
            throw new CouponValidationException("COUPON_USAGE_LIMIT_REACHED", "This coupon has reached its usage limit.");

        if (coupon.UsageLimitPerUser.HasValue)
        {
            var userUsageCount = await db.Orders
                .CountAsync(o =>
                    o.UserId == userId &&
                    o.CouponCode == normalizedCode &&
                    o.Status != OrderStatus.Cancelled, ct);

            if (userUsageCount >= coupon.UsageLimitPerUser.Value)
                throw new CouponValidationException("COUPON_USER_LIMIT_REACHED", "You have already used this coupon the maximum number of times.");
        }

        var cart = await GetOrCreateCartAsync(userId, ct);
        var subTotal = CalculateSubTotal(cart);

        if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            throw new CouponValidationException("COUPON_MIN_ORDER_NOT_MET",
                $"A minimum order of ₹{coupon.MinOrderAmount.Value:F0} is required to use this coupon.");

        var discount = coupon.DiscountType == DiscountType.Percentage
            ? subTotal * coupon.DiscountValue / 100m
            : coupon.DiscountValue;

        if (coupon.MaxDiscountCap.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountCap.Value);

        return MapCart(cart, normalizedCode, discount);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<CartEntity> GetOrCreateCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null) return cart;

        cart = new CartEntity { UserId = userId };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return cart;
    }

    private async Task<CartEntity?> LoadCartAsync(Guid cartId, CancellationToken ct) =>
        await db.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

    private static decimal CalculateSubTotal(CartEntity cart) =>
        cart.Items.Sum(i =>
        {
            var price = i.ProductVariant.PriceOverride
                     ?? i.ProductVariant.Product.DiscountedPrice
                     ?? i.ProductVariant.Product.BasePrice;
            return price * i.Quantity;
        });

    private static CartDto MapCart(CartEntity cart, string? couponCode, decimal discount)
    {
        var items = cart.Items.Select(i =>
        {
            var unitPrice = i.ProductVariant.PriceOverride
                         ?? i.ProductVariant.Product.DiscountedPrice
                         ?? i.ProductVariant.Product.BasePrice;
            var imageUrl = i.ProductVariant.Product.Images
                .OrderBy(img => img.DisplayOrder)
                .Select(img => img.Url)
                .FirstOrDefault();
            return new CartItemDto(
                i.Id,
                i.ProductVariantId,
                i.ProductVariant.ProductId,
                i.ProductVariant.Product.Name,
                imageUrl,
                i.ProductVariant.Size,
                i.ProductVariant.Colour,
                unitPrice,
                i.Quantity,
                unitPrice * i.Quantity
            );
        }).ToList();

        var subTotal = items.Sum(i => i.TotalPrice);
        var total    = Math.Max(subTotal - discount, 0m);
        return new CartDto(cart.Id, items, subTotal, discount, total, couponCode);
    }
}
