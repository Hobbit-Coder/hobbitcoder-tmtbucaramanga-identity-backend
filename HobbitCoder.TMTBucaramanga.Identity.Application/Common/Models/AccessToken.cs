namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

public sealed class AccessToken
{
    /// <summary>JWT firmado con RS256.</summary>
    public required string Token { get; set; }

    /// <summary>Momento del claim <c>exp</c>.</summary>
    public required DateTimeOffset ExpiresAt { get; set; }
}
