using StyleNest.SharedKernel.Domain;

namespace StyleNest.Infrastructure.Entities.Catalog;

public class AttributeDefinition : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "Text"; // Text, Number, Boolean, Select
    public bool IsFilterable { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public string? AllowedValues { get; set; } // JSON array for Select type

    public ICollection<CategoryAttribute> CategoryAttributes { get; set; } = [];
    public ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
    public ICollection<ProductVariantOption> VariantOptions { get; set; } = [];
}
