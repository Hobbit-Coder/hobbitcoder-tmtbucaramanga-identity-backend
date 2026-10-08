namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// Emisión de los JWT de la plataforma, leída de la sección <see cref="SectionName"/> y validada al
/// arrancar: la aplicación no inicia con un emisor o una clave de firma inválidos.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// URL base pública de Identity (por ejemplo <c>https://identity.tmtbucaramanga.com</c>), sin barra
    /// final. Se usa tal cual en el claim <c>iss</c> y en el <c>issuer</c> del discovery, y es la misma
    /// URL que las APIs consumidoras configuran como <c>Authority</c>.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>APIs registradas: valores admitidos para el claim <c>aud</c>.</summary>
    public List<string> Audiences { get; set; } = [];

    /// <summary>
    /// Aplicaciones (frontends) que inician sesión con Identity mediante código de autorización y PKCE.
    /// </summary>
    public List<ClientSettings> Clients { get; set; } = [];

    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Vida de cada refresh token. Cada uso lo rota por uno nuevo con la vida completa, así que una
    /// sesión sigue abierta mientras se use al menos una vez en este plazo.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>Clave RSA privada que firma los tokens nuevos.</summary>
    public SigningKeySettings SigningKey { get; set; } = new();

    /// <summary>
    /// Claves que ya no firman pero se siguen publicando en el JWKS hasta que expiren los últimos
    /// tokens que firmaron. Basta con la clave pública.
    /// </summary>
    public List<SigningKeySettings> PreviousSigningKeys { get; set; } = [];
}

/// <summary>
/// Aplicación cliente registrada. Identity solo devuelve códigos de autorización a sus
/// <see cref="RedirectUris"/> y emite sus tokens para <see cref="Audience"/>.
/// </summary>
public class ClientSettings
{
    /// <summary>Identificador público del cliente (<c>client_id</c>).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>API de los tokens de acceso del cliente. Debe estar en <see cref="JwtSettings.Audiences"/>.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>URIs exactas de retorno: absolutas, sin query ni fragmento y con <c>https</c> fuera de desarrollo.</summary>
    public List<string> RedirectUris { get; set; } = [];
}

/// <summary>
/// Clave RSA en formato PEM. Se indica <see cref="Pem"/> o <see cref="PemPath"/>, no ambos.
/// Nunca va en el repositorio ni en <c>appsettings*.json</c>.
/// </summary>
public class SigningKeySettings
{
    /// <summary>
    /// Valor del <c>kid</c>. Si se omite se usa el thumbprint de la clave (RFC 7638), que no cambia
    /// mientras la clave sea la misma.
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>Contenido PEM de la clave.</summary>
    public string? Pem { get; set; }

    /// <summary>Ruta a un archivo PEM fuera del repositorio. Una ruta relativa se resuelve desde el content root.</summary>
    public string? PemPath { get; set; }
}
