using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.Application;

/// <summary>El teléfono identifica la cuenta al ingresar: se guarda y se busca siempre igual.</summary>
public class PhoneNumbersTests
{
    [Theory]
    [InlineData("300 000 0101", "+573000000101")]
    [InlineData("+57 300-000-0101", "+573000000101")]
    [InlineData("(300) 000.0101", "+573000000101")]
    [InlineData("+1 415 555 0101", "+14155550101")]
    public void Normalize_ReturnsDigitsWithCountryCode(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumbers.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ana.perez")]
    [InlineData("ana@correo.com")]
    [InlineData("123")]
    public void Normalize_WithSomethingThatIsNotAPhone_ReturnsNull(string input)
    {
        Assert.Null(PhoneNumbers.Normalize(input));
    }
}
