using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

/// <summary>
/// Confirmación de identidad del titular mediante un código enviado a su teléfono (§6). Se confirma con el
/// código y entrega un token de verificación de un solo uso con el que el titular crea su contraseña.
/// </summary>
/// <remarks>Solo se guardan hashes del código y del token.</remarks>
public class VerificationRequest : BaseEntity
{
    public required Guid UserId { get; set; }
    public required VerificationPurpose Purpose { get; set; }
    public required VerificationChannel Channel { get; set; }

    /// <summary>Hash SHA-256 (hexadecimal) del código enviado.</summary>
    public required string CodeHash { get; set; }

    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Hash del token que se entregó al confirmar el código.</summary>
    public string? ConfirmationTokenHash { get; set; }

    /// <summary>Momento en que el token se usó para crear la contraseña.</summary>
    public DateTime? CompletedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public bool CanConfirm(DateTime now, int maxAttempts) =>
        ConfirmedAt is null && CompletedAt is null && ExpiresAt > now && FailedAttempts < maxAttempts;

    public bool CanComplete(DateTime now) => ConfirmedAt is not null && CompletedAt is null && ExpiresAt > now;
}
