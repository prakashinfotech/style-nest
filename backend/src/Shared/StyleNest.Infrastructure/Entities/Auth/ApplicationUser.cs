using Microsoft.AspNetCore.Identity;

namespace StyleNest.Infrastructure.Entities.Auth;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // ENH-AUTH-005: tracks the most-recent lockout duration for exponential doubling (0 = never locked)
    public int LockoutDurationSeconds { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserAddress> Addresses { get; set; } = [];
}
