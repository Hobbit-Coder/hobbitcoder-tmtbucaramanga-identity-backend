using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using System.Globalization;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.Domain;

/// <summary>Edad = años cumplidos a partir de la fecha de nacimiento (§5, HU-01).</summary>
public class UserAgeTests
{
    [Theory]
    [InlineData("1998-03-15", "2026-10-08", 28)]
    [InlineData("2008-10-09", "2026-10-08", 17)]
    [InlineData("2008-10-08", "2026-10-08", 18)]
    [InlineData("2004-02-29", "2026-03-01", 22)]
    public void CalculateAge_CountsOnlyCompletedYears(string birthDate, string today, int expected)
    {
        Assert.Equal(expected, User.CalculateAge(DateOnly.Parse(birthDate, CultureInfo.InvariantCulture), DateOnly.Parse(today, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void AgeOn_WithoutBirthDate_IsNull()
    {
        var user = new User { FirstName = "Ana", LastName = "Pérez", Role = default, IsActive = true };

        Assert.Null(user.AgeOn(new DateOnly(2026, 10, 8)));
    }
}
