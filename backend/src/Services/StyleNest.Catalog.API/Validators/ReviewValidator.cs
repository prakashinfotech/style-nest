using FluentValidation;
using StyleNest.Catalog.API.DTOs;

namespace StyleNest.Catalog.API.Validators;

public sealed class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Review title is required.")
            .MaximumLength(120).WithMessage("Title must not exceed 120 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Review body is required.")
            .MaximumLength(2000).WithMessage("Review body must not exceed 2000 characters.");

        // ENH-PDP-008 — photo URLs: optional, max 4, each max 500 chars
        RuleFor(x => x.PhotoUrls)
            .Must(urls => urls == null || urls.Count <= 4)
            .WithMessage("A maximum of 4 photo URLs may be attached to a review.");

        RuleForEach(x => x.PhotoUrls)
            .MaximumLength(500).WithMessage("Each photo URL must not exceed 500 characters.")
            .When(x => x.PhotoUrls != null);
    }
}
