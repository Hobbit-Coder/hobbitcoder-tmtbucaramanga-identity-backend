namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

/// <summary>
/// Datos de un token de acceso. El payload del JWT solo va firmado, no cifrado: no agregues aquí
/// datos sensibles.
/// </summary>
/// <param name="SessionId">
/// Sesión de Identity que emitió el token (claim <c>sid</c>, el <c>FamilyId</c> de sus refresh tokens).
/// Todos los tokens de una sesión lo llevan; Identity lo usa para unir otras aplicaciones a esa sesión.
/// </param>
public sealed class AccessTokenRequest
{
    /// <summary>Id único y estable del usuario (claim <c>sub</c>).</summary>
    public required string Subject { get; set; }

    /// <summary>Identificador de la API destino (claim <c>aud</c>). Debe estar registrado en <c>Jwt:Audiences</c>.</summary>
    public required string Audience { get; set; }

    /// <summary>Roles del usuario; cada uno se emite en el claim <c>role</c>.</summary>
    public required IReadOnlyCollection<string> Roles { get; set; }

    /// <summary>/// Sesión de Identity que emitió el token (claim <c>sid</c>, el <c>FamilyId</c> de sus refresh tokens).
/// Todos los tokens de una sesión lo llevan; Identity lo usa para unir otras aplicaciones a esa sesión.
///</summary>
    public string? SessionId { get; set; }

    /// <summary>Capacidades puntuales (claim <c>permission</c>), por ejemplo registrar asistentes.</summary>
    public IReadOnlyCollection<string>? Permissions { get; set; }
}
