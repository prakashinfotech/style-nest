using FluentValidation;
using StyleNest.Admin.API.DTOs;

namespace StyleNest.Admin.API.Validators;

public sealed class AdminOrderStatusValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Confirmed", "Processing", "Shipped", "OutForDelivery", "Delivered", "Cancelled"
    };

    public AdminOrderStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be one of: Confirmed, Processing, Shipped, OutForDelivery, Delivered, Cancelled.");
    }
}
