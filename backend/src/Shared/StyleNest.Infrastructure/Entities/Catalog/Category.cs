using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class Category : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<CategoryAttribute> CategoryAttributes { get; set; } = [];
}
