namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;

/// <summary>
/// Valores del claim <c>role</c>. Coinciden con los nombres de <see cref="Enums.UserRole"/>: Identity los
/// emite y las APIs (Sheepfold) los leen como <c>ClaimTypes.Role</c>.
/// </summary>
public static class RoleNames
{
    /// <summary>Administra cuentas y configuración; no participa en la operación ministerial.</summary>
    public const string Admin = "Admin";
    public const string Director = "Director";
    public const string Coordinator = "Coordinator";
    public const string Leader = "Leader";
    public const string Apprentice = "Apprentice";

    /// <summary>Aplicaciones (no personas) autenticadas con credenciales de servicio, como la API de Sheepfold.</summary>
    public const string Service = "Service";

    /// <summary>Quienes administran cuentas (§4.1 y §4.5 del requerimiento).</summary>
    public const string AccountManagers = Admin + "," + Director;
}
