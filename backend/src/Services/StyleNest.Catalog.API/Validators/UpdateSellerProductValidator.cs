using FluentValidation;
using StyleNest.Catalog.API.DTOs;

namespace StyleNest.Catalog.API.Validators;

public sealed class UpdateSellerProductValidator : AbstractValidator<UpdateSellerProductRequest>
{
    public UpdateSellerProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Selling price must be greater than zero.");
        RuleFor(x => x.Mrp)
            .GreaterThan(x => x.Price).WithMessage("MRP must be greater than the selling price.")
            .When(x => x.Mrp.HasValue);
    }
}
