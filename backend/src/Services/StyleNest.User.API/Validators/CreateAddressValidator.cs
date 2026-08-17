using FluentValidation;
using StyleNest.User.API.DTOs;

namespace StyleNest.User.API.Validators;

public class CreateAddressValidator : AbstractValidator<CreateAddressRequestDto>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\d{10}$").WithMessage("Phone number must be 10 digits.");
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PinCode).NotEmpty().Matches(@"^\d{6}$").WithMessage("Pin code must be 6 digits.");
    }
}
