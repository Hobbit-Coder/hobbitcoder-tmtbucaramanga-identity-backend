namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

/// <summary>
/// Datos de un token de acceso. El payload del JWT solo va firmado, no cifrado: no agregues aquí
/// datos sensibles.
/// </summary>
/// <param name="Subject">Id único y estable del usuario (claim <c>sub</c>).</param>
/// <param name="Audience">Identificador de la API destino (claim <c>aud</c>). Debe estar registrado en <c>Jwt:Audiences</c>.</param>
/// <param name="Roles">Roles del usuario; cada uno se emite en el claim <c>role</c>.</param>
/// <param name="SessionId">
/// Sesión de Identity que emitió el token (claim <c>sid</c>, el <c>FamilyId</c> de sus refresh tokens).
/// Todos los tokens de una sesión lo llevan; Identity lo usa para unir otras aplicaciones a esa sesión.
/// </param>
/// <param name="Permissions">Capacidades puntuales (claim <c>permission</c>), por ejemplo registrar asistentes.</param>
public sealed record AccessTokenRequest(
    string Subject,
    string Audience,
    IReadOnlyCollection<string> Roles,
    string? SessionId = null,
    IReadOnlyCollection<string>? Permissions = null);
