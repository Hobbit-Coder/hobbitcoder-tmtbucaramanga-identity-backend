using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

/// <summary>
/// Código de autorización (OAuth 2.0 con PKCE) que Identity entrega a una aplicación cliente, como
/// Sheepfold, después de que el usuario inicia sesión en Identity. La aplicación lo cambia una sola vez
/// por un par de tokens que pertenecen a la misma sesión de Identity.
/// </summary>
/// <remarks>
/// Solo se guarda el hash SHA-256 del código. Vive muy poco y se marca como usado en el primer intento
/// de canje, sea exitoso o no.
/// </remarks>
public class AuthorizationCode : BaseEntity
{
    public required Guid UserId { get; set; }

    /// <summary>Sesión de Identity (el <see cref="RefreshToken.FamilyId"/> de sus refresh tokens) a la que se une el cliente.</summary>
    public required Guid FamilyId { get; set; }

    public required string ClientId { get; set; }

    /// <summary>URI a la que se envió el código; el canje debe presentar exactamente la misma.</summary>
    public required string RedirectUri { get; set; }

    /// <summary>Hash SHA-256 (hexadecimal) del código.</summary>
    public required string CodeHash { get; set; }

    /// <summary>Desafío PKCE (S256) que debe cumplir el <c>code_verifier</c> del canje.</summary>
    public required string CodeChallenge { get; set; }

    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public bool IsUsable(DateTime now) => ConsumedAt is null && ExpiresAt > now;

    public void Consume(DateTime now) => ConsumedAt ??= now;
}
