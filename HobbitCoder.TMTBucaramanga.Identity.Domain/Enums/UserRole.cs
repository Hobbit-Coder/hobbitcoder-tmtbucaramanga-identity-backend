namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

/// <summary>Rol de la cuenta (§3.1). Cada usuario tiene uno; se guarda por nombre.</summary>
public enum UserRole
{
    Admin = 1,
    Director = 2,
    Coordinator = 3,
    Leader = 4,
    Apprentice = 5
}
