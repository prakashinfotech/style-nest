using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Auth;

public enum OtpPurpose { PasswordReset, EmailVerification, PhoneVerification, MfaVerification }

public class OtpCode : BaseEntity<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public int Attempts { get; set; } = 0;
}
