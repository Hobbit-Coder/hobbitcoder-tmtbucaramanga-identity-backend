using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

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
