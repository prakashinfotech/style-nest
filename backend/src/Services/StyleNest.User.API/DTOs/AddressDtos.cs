namespace StyleNest.User.API.DTOs;

public record AddressResponseDto(
    Guid Id,
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PinCode,
    bool IsDefault);

public record CreateAddressRequestDto(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PinCode,
    bool IsDefault);

public record UpdateAddressRequestDto(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PinCode,
    bool IsDefault);
