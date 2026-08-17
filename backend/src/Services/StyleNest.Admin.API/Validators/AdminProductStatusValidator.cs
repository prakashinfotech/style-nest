using FluentValidation;
using StyleNest.Admin.API.DTOs;

namespace StyleNest.Admin.API.Validators;

public sealed class AdminProductStatusValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public AdminProductStatusValidator()
    {
        RuleFor(x => x.IsActive).NotNull();
    }
}
