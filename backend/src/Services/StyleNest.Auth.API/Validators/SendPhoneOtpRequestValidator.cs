using FluentValidation;
using StyleNest.Auth.API.DTOs;

namespace StyleNest.Auth.API.Validators;

public sealed class SendPhoneOtpRequestValidator : AbstractValidator<SendPhoneOtpRequest>
{
    public SendPhoneOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+91[6-9]\d{9}$")
            .WithMessage("PhoneNumber must be a valid Indian mobile number in E.164 format (+91XXXXXXXXXX).");
    }
}
