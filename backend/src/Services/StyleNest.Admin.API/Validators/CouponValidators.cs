using FluentValidation;
using StyleNest.Admin.API.DTOs;

namespace StyleNest.Admin.API.Validators;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Coupon code must be uppercase alphanumeric.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.MinOrderAmount).GreaterThan(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.MaxDiscountCap).GreaterThan(0).When(x => x.MaxDiscountCap.HasValue);
        RuleFor(x => x.UsageLimitPerUser).GreaterThan(0).When(x => x.UsageLimitPerUser.HasValue);
        RuleFor(x => x.TotalUsageLimit).GreaterThan(0).When(x => x.TotalUsageLimit.HasValue);
        RuleFor(x => x.ExpiresAt).GreaterThan(x => x.StartsAt)
            .When(x => x.StartsAt.HasValue && x.ExpiresAt.HasValue);
    }
}

public sealed class UpdateCouponValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.MinOrderAmount).GreaterThan(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.MaxDiscountCap).GreaterThan(0).When(x => x.MaxDiscountCap.HasValue);
        RuleFor(x => x.UsageLimitPerUser).GreaterThan(0).When(x => x.UsageLimitPerUser.HasValue);
        RuleFor(x => x.TotalUsageLimit).GreaterThan(0).When(x => x.TotalUsageLimit.HasValue);
        RuleFor(x => x.ExpiresAt).GreaterThan(x => x.StartsAt)
            .When(x => x.StartsAt.HasValue && x.ExpiresAt.HasValue);
    }
}
