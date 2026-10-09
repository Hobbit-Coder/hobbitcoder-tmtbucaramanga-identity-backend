using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

/// <summary>
/// Cambio realizado sobre una cuenta (§25: usuarios, activaciones, desactivaciones y eliminaciones).
/// Es un hecho histórico: guarda el nombre de la cuenta y los valores tal como eran en ese momento, y no
/// referencia la cuenta con clave foránea para sobrevivir a su eliminación definitiva.
/// </summary>
public class AccountAuditEntry : BaseEntity
{
    public required DateTime OccurredAt { get; set; }

    /// <summary>Usuario que realizó la acción (claim <c>sub</c>) o <c>System</c>.</summary>
    public required string ActorUserId { get; set; }

    public required AccountAuditAction Action { get; set; }
    public required Guid TargetUserId { get; set; }

    /// <summary>Nombre de la cuenta afectada en ese momento.</summary>
    public required string TargetName { get; set; }

    /// <summary>Datos anteriores (JSON); nunca contraseñas ni hashes.</summary>
    public string? Before { get; set; }

    /// <summary>Datos resultantes (JSON); nunca contraseñas ni hashes.</summary>
    public string? After { get; set; }
}
