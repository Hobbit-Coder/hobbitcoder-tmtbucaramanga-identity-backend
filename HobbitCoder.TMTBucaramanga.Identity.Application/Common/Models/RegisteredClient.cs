namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

/// <param name="ClientId">Identificador público del cliente (<c>client_id</c>).</param>
/// <param name="Audience">API para la que se emiten sus tokens de acceso. Debe estar en <c>Jwt:Audiences</c>.</param>
/// <param name="RedirectUris">URIs exactas a las que Identity puede devolver el código.</param>
public sealed record RegisteredClient(string ClientId, string Audience, IReadOnlyCollection<string> RedirectUris)
{
    /// <summary>
    /// Solo se aceptan URIs registradas, comparadas carácter por carácter: así un código nunca se envía
    /// a un sitio que no sea el cliente.
    /// </summary>
    public bool AllowsRedirectUri(string redirectUri) => RedirectUris.Contains(redirectUri, StringComparer.Ordinal);
}
