using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class ProductVariantOption : BaseEntity<Guid>
{
    public Guid ProductVariantId { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;

    public ProductVariant ProductVariant { get; set; } = null!;
    public AttributeDefinition AttributeDefinition { get; set; } = null!;
}
