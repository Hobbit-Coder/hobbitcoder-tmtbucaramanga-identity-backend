using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

/// <summary>
/// Refresh token opaco de una sesión. Solo se guarda el hash SHA-256 del token: el valor en claro lo
/// tiene únicamente el cliente. Las APIs consumidoras nunca lo reciben ni lo validan.
/// </summary>
/// <remarks>
/// Cada uso lo rota: el token usado queda revocado (<see cref="RefreshTokenRevocationReason.Rotated"/>)
/// y se emite otro en la misma familia. Todos los tokens rotados desde un mismo inicio de sesión comparten
/// <see cref="FamilyId"/>; si se presenta un token ya rotado se revoca toda la familia, porque significa
/// que alguien más tiene una copia.
/// </remarks>
public class RefreshToken : BaseEntity
{
    public required Guid UserId { get; set; }

    /// <summary>Sesión a la que pertenece: el mismo valor para todos los tokens rotados desde un login.</summary>
    public required Guid FamilyId { get; set; }

    /// <summary>Hash SHA-256 (hexadecimal) del token.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Audiencia por defecto de los tokens de acceso que se emiten al usarlo.</summary>
    public required string Audience { get; set; }

    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public RefreshTokenRevocationReason? RevocationReason { get; set; }

    public virtual User User { get; set; } = null!;

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTime now, RefreshTokenRevocationReason reason)
    {
        RevokedAt ??= now;
        RevocationReason ??= reason;
    }
}
