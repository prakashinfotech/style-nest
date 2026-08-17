using StyleNest.Infrastructure.Entities.Catalog;
using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Commerce;

public class WishlistItem : BaseEntity<Guid>
{
    public Guid WishlistId { get; set; }
    public Guid ProductId { get; set; }

    public Wishlist Wishlist { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
