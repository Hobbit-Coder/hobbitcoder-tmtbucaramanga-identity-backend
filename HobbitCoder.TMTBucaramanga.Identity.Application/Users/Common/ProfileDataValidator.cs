using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public class ProfileDataValidator : AbstractValidator<IProfileData>
{
    public ProfileDataValidator(TimeProvider timeProvider)
    {
        RuleFor(profile => profile.FirstName).ValidName("FirstName");
        RuleFor(profile => profile.LastName).ValidName("LastName");
        RuleFor(profile => profile.Gender)
            .NotNull().WithMessage("Gender is required")
            .IsInEnum().WithMessage("Gender is not valid");
        RuleFor(profile => profile.PhoneNumber).ValidPhoneNumber();
        RuleFor(profile => profile.Email).OptionalEmail().When(profile => !string.IsNullOrWhiteSpace(profile.Email));
        RuleFor(profile => profile.UserName).ValidUserName();
        RuleFor(profile => profile.Instagram)
            .NotEmpty().WithMessage("Instagram is required")
            .ValidInstagram();
        RuleFor(profile => profile.BirthDate)
            .NotNull().WithMessage("BirthDate is required")
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("BirthDate cannot be in the future");
    }
}
