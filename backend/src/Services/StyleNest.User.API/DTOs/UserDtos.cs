namespace StyleNest.User.API.DTOs;

public record UserProfileResponseDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime? DateOfBirth,
    string? AvatarUrl,
    DateTime CreatedAt);

public record UpdateProfileRequestDto(
    string FirstName,
    string LastName,
    DateTime? DateOfBirth);
