using FluentValidation;
using StyleNest.Admin.API.DTOs;

namespace StyleNest.Admin.API.Validators;

public sealed class CreateBannerValidator : AbstractValidator<CreateBannerRequest>
{
    public CreateBannerValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.LinkUrl).MaximumLength(500).When(x => x.LinkUrl is not null);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);
    }
}

public sealed class UpdateBannerValidator : AbstractValidator<UpdateBannerRequest>
{
    public UpdateBannerValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.LinkUrl).MaximumLength(500).When(x => x.LinkUrl is not null);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt)
            .When(x => x.StartsAt.HasValue && x.EndsAt.HasValue);
    }
}
