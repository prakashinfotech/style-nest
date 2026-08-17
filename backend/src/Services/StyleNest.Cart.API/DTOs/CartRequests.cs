namespace StyleNest.Cart.API.DTOs;

public record AddToCartRequest(Guid ProductId, string? Size, string? Colour, int Quantity);

public record UpdateCartItemRequest(int Quantity);

public record ApplyCouponRequest(string Code);
