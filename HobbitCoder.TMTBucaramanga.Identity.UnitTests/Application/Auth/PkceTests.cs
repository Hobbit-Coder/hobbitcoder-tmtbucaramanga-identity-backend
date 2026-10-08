using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using System.Text.RegularExpressions;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.Application.Auth;

public class PkceTests
{
    /// <summary>Ejemplo del apéndice B de RFC 7636.</summary>
    private const string CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string CodeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    [Fact]
    public void IsValid_WithTheVerifierOfTheChallenge_ReturnsTrue()
    {
        Assert.True(Pkce.IsValid(CodeVerifier, CodeChallenge));
    }

    [Fact]
    public void IsValid_WithAnotherVerifier_ReturnsFalse()
    {
        Assert.False(Pkce.IsValid("otro-verifier-otro-verifier-otro-verifier-123", CodeChallenge));
    }

    [Fact]
    public void Patterns_AcceptTheRfcExample()
    {
        Assert.Matches(new Regex(Pkce.CodeVerifierPattern), CodeVerifier);
        Assert.Matches(new Regex(Pkce.CodeChallengePattern), CodeChallenge);
    }
}
