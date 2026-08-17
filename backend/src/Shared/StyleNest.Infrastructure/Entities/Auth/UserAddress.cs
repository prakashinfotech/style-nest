using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Auth;

public class UserAddress : BaseEntity<Guid>
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
