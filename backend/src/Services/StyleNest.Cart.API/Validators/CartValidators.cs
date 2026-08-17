using FluentValidation;
using StyleNest.Cart.API.DTOs;

namespace StyleNest.Cart.API.Validators;

public sealed class AddToCartValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
    }
}

public sealed class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
    }
}

public sealed class ApplyCouponValidator : AbstractValidator<ApplyCouponRequest>
{
    public ApplyCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}
