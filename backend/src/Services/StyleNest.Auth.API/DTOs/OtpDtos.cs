namespace StyleNest.Auth.API.DTOs;

public record SendPhoneOtpRequest(string PhoneNumber);
public record SendPhoneOtpResponse(string MaskedPhone, DateTime ExpiresAt);

public record ForgotPasswordRequest(string Email);

public record VerifyOtpRequest(string Email, string Code, string Purpose);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record CreateAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    string Password
);

public record CreateSellerRequest(
    string Email,
    string FirstName,
    string LastName,
    string StoreName,
    string Password
);

public record CreateUserResponse(Guid UserId, string Email, string Role, DateTime CreatedAt);
