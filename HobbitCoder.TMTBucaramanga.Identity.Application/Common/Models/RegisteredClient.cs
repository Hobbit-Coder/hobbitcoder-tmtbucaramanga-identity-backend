namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

public sealed class RegisteredClient
{
    /// <summary>Identificador público del cliente (<c>client_id</c>).</summary>
    public required string ClientId { get; set; }

    /// <summary>API para la que se emiten sus tokens de acceso. Debe estar en <c>Jwt:Audiences</c>.</summary>
    public required string Audience { get; set; }

    /// <summary>URIs exactas a las que Identity puede devolver el código.</summary>
    public required IReadOnlyCollection<string> RedirectUris { get; set; }

    /// <summary>
    /// Solo se aceptan URIs registradas, comparadas carácter por carácter: así un código nunca se envía
    /// a un sitio que no sea el cliente.
    /// </summary>
    public bool AllowsRedirectUri(string redirectUri) => RedirectUris.Contains(redirectUri, StringComparer.Ordinal);
}
