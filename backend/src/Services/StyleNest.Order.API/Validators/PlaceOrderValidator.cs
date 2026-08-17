using FluentValidation;
using StyleNest.Order.API.DTOs;

namespace StyleNest.Order.API.Validators;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Pincode).NotEmpty().Matches(@"^\d{6}$").WithMessage("Pincode must be 6 digits.");
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
    }
}
