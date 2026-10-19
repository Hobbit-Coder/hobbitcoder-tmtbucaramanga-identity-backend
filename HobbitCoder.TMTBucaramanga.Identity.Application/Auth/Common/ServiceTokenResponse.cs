namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;

public sealed class ServiceTokenResponse
{
    public required string AccessToken { get; set; }
    public required string TokenType { get; set; }
    public required int ExpiresIn { get; set; }
}
