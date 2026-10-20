using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Seeding;
using System.Text.RegularExpressions;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.Infrastructure;

public class IdentitySeedTests
{
    private static readonly IdentitySeed Seed = IdentitySeed.Load();

    [Fact]
    public void LoadsDirectorsCoordinatorsAndLeaders()
    {
        Assert.Equal(2, Seed.Users.Count(user => user.Role == UserRole.Director));
        Assert.Equal(4, Seed.Users.Count(user => user.Role == UserRole.Coordinator));
        Assert.Contains(Seed.Users, user => user.Role == UserRole.Leader);
        Assert.DoesNotContain(Seed.Users, user => user.Role is UserRole.Admin or UserRole.Apprentice);
    }

    [Fact]
    public void IdentifiersAreValidAndUnique()
    {
        Assert.All(Seed.Users, user =>
        {
            Assert.Equal(user.PhoneNumber, PhoneNumbers.Normalize(user.PhoneNumber));
            Assert.Matches(new Regex("^[a-z.]{3,50}$"), user.UserName);
            Assert.True(user.IsActive);
            Assert.Null(user.PasswordHash);
        });
        Assert.Equal(Seed.Users.Count, Seed.Users.Select(user => user.Id).Distinct().Count());
        Assert.Equal(Seed.Users.Count, Seed.Users.Select(user => user.PhoneNumber).Distinct().Count());
        Assert.Equal(Seed.Users.Count, Seed.Users.Select(user => user.UserName).Distinct().Count());
    }
}
