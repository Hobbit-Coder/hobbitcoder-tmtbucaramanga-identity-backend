using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Emite los JWT de acceso de la plataforma. Identity es el único servicio que los firma; las APIs
/// consumidoras solo los validan con las claves públicas de su JWKS.
/// </summary>
public interface IAccessTokenIssuer
{
    /// <exception cref="ArgumentException">
    /// El <see cref="AccessTokenRequest.Subject"/> está vacío o la <see cref="AccessTokenRequest.Audience"/> no está registrada.
    /// </exception>
    AccessToken Issue(AccessTokenRequest request);

    /// <summary>Indica si <paramref name="audience"/> es una API registrada en <c>Jwt:Audiences</c>.</summary>
    bool IsRegisteredAudience(string audience);
}
