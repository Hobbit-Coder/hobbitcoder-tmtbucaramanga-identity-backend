namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

/// <summary>Rol de la cuenta. Cada usuario tiene uno; se guarda por nombre.</summary>
public enum UserRole
{
    Admin = 1,
    Director = 2,
    Coordinator = 3,
    Leader = 4,
    Apprentice = 5,

    /// <summary>Del ministerio sin estar en la jerarquía: administra los asistentes vigentes.</summary>
    Sniper = 6
}
