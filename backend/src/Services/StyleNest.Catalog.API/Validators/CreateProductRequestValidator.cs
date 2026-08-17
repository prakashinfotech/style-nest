using FluentValidation;
using StyleNest.Catalog.API.DTOs;

namespace StyleNest.Catalog.API.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
        RuleFor(x => x.BrandId).NotEmpty().WithMessage("BrandId is required.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("CategoryId is required.");
        RuleFor(x => x.SalePrice)
            .LessThan(x => x.Price).WithMessage("Sale price must be less than the regular price.")
            .When(x => x.SalePrice.HasValue);
        RuleFor(x => x.ImageUrls).NotEmpty().WithMessage("At least one image URL is required.");
    }
}
