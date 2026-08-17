using FluentValidation;
using StyleNest.Seller.API.DTOs;

namespace StyleNest.Seller.API.Validators;

public class UpdateSellerProfileValidator : AbstractValidator<UpdateSellerProfileRequest>
{
    public UpdateSellerProfileValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000);
    }
}

public class CreateSellerProductValidator : AbstractValidator<CreateSellerProductRequest>
{
    public CreateSellerProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty);
        RuleFor(x => x.BrandId).NotEqual(Guid.Empty);
        RuleFor(x => x.Variants).NotEmpty().WithMessage("At least one variant is required.");
        RuleFor(x => x.ImageUrls).NotEmpty().WithMessage("At least one image is required.");
    }
}

public class UpdateInventoryValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryValidator()
    {
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
    }
}

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    private static readonly string[] ValidStatuses =
        ["Pending", "Confirmed", "Processing", "Shipped", "OutForDelivery", "Delivered", "Cancelled"];

    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}
