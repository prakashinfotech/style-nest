using StyleNest.Infrastructure.Entities.Auth;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Commerce;

public class Wishlist : BaseEntity<Guid>
{
    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<WishlistItem> Items { get; set; } = [];
}
