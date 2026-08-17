using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class ProductAttribute : BaseEntity<Guid>
{
    public Guid ProductId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;

    public Product Product { get; set; } = null!;
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
}
