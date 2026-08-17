namespace StyleNest.Order.API.DTOs;

public record PlaceOrderRequest(
    string  AddressLine1,
    string? AddressLine2,
    string  City,
    string  State,
    string  Pincode,
    string  PaymentMethod,
    string? CouponCode
);
