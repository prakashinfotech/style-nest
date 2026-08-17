using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Admin;

public enum BannerPlacement
{
    HeroCarousel,
    CategoryStrip,
    PromoBanner,
    FlashSale
}

public class Banner : BaseEntity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public BannerPlacement Placement { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
