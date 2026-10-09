namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;

/// <summary>Valores del claim <c>permission</c>: capacidades puntuales que no dependen del rol.</summary>
public static class PermissionNames
{
    public const string ClaimType = "permission";

    /// <summary>Designación para registrar asistentes nuevos (§7). No amplía el alcance de edición.</summary>
    public const string RegisterAttendees = "RegisterAttendees";
}
