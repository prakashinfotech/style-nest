namespace StyleNest.Auth.API.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);

public record RefreshRequestDto(string RefreshToken);
