using StyleNest.SharedKernel.Domain;

namespace StyleNest.Auth.API.Services;

public record PhoneOtpSentResult(string MaskedPhone, DateTime ExpiresAt);

public interface IOtpService
{
    Task<Result> SendForgotPasswordOtpAsync(string email);
    Task<Result> VerifyOtpAsync(string email, string code, string purpose);
    Task<Result> ResetPasswordAsync(string email, string code, string newPassword);
    Task<Result<PhoneOtpSentResult>> SendPhoneOtpAsync(string phoneNumber);
    Task<Result> VerifyPhoneOtpAsync(string phoneNumber, string code);
}
