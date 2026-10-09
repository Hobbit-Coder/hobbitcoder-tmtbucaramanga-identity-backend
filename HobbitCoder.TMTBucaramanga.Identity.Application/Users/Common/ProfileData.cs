using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>
/// Perfil que el titular completa al activar su cuenta y mantiene en "Mi perfil" (§5). Todos los datos son
/// obligatorios menos el correo (HU-01: "el correo es opcional; los demás datos definidos para el perfil
/// deben quedar completos").
/// </summary>
public interface IProfileData
{
    string FirstName { get; }
    string LastName { get; }
    Gender? Gender { get; }
    string PhoneNumber { get; }
    string? Email { get; }
    string UserName { get; }
    string? Instagram { get; }
    DateOnly? BirthDate { get; }
}

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

public static class ProfileDataExtensions
{
    public static void ApplyProfile(this User user, IProfileData profile, ILookupNormalizer normalizer, string phoneNumber, string? email)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(profile);

        user.FirstName = profile.FirstName.Trim();
        user.LastName = profile.LastName.Trim();
        user.Gender = profile.Gender;
        user.Instagram = profile.Instagram.EmptyAsNull();
        user.BirthDate = profile.BirthDate;
        user.SetIdentifiers(normalizer, profile.UserName, phoneNumber, email);
    }
}
