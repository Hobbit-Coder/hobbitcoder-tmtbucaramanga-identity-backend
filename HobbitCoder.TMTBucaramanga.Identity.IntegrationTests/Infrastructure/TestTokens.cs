using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

public static class TestTokens
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <summary>
    /// Arma un token a mano, para probar valores que el emisor de Identity nunca produce.
    /// </summary>
    public static string Create(SigningCredentials signingCredentials, string issuer, string audience, string subject = "usuario-de-prueba")
    {
        var now = DateTime.UtcNow;

        return TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = subject },
            SigningCredentials = signingCredentials
        });
    }

    /// <summary>
    /// Emisor de Identity con su configuración y sus claves, pero con otro reloj: emite tokens reales en
    /// otro momento (por ejemplo, ya expirados).
    /// </summary>
    public static JwtAccessTokenIssuer CreateIssuerAt(IServiceProvider services, DateTimeOffset now) =>
        new(services.GetRequiredService<IOptions<JwtSettings>>(), services.GetRequiredService<SigningKeyStore>(), new FixedTimeProvider(now));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
