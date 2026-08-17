using FluentValidation;
using StyleNest.Admin.API.DTOs;

namespace StyleNest.Admin.API.Validators;

public sealed class CreateSellerRequestValidator : AbstractValidator<CreateSellerRequest>
{
    public CreateSellerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().Length(2, 50);
        RuleFor(x => x.LastName).NotEmpty().Length(2, 50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
