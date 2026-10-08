namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

/// <param name="Token">JWT firmado con RS256.</param>
/// <param name="ExpiresAt">Momento del claim <c>exp</c>.</param>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
