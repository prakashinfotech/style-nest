using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Auth;

public class RefreshToken : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }

    // ENH-AUTH-004: session metadata for multi-device management
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
