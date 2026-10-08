using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;

public static class CurrentUserServiceExtensions
{
    /// <summary>
    /// Id del usuario autenticado (claim <c>sub</c>). Solo tiene sentido en endpoints con
    /// <c>[Authorize]</c>; sin usuario lanza <see cref="UnauthorizedAccessException"/> (401).
    /// </summary>
    public static Guid GetRequiredUserId(this ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);

        return Guid.TryParse(currentUserService.UserId, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("No hay un usuario autenticado.");
    }

    /// <summary>
    /// Sesión de Identity del token (claim <c>sid</c>). Solo tiene sentido en endpoints con
    /// <c>[Authorize]</c>; un token sin sesión lanza <see cref="UnauthorizedAccessException"/> (401).
    /// </summary>
    public static Guid GetRequiredSessionId(this ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);

        return Guid.TryParse(currentUserService.SessionId, out var sessionId)
            ? sessionId
            : throw new UnauthorizedAccessException("El token no pertenece a una sesión de Identity.");
    }
}
