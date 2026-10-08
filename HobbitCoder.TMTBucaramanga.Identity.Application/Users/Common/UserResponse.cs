namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public class UserResponse
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public required bool IsActive { get; set; }
    public required bool EmailConfirmed { get; set; }

    /// <summary>Fin del bloqueo por intentos fallidos de inicio de sesión; <c>null</c> si no está bloqueado.</summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    public required DateTime CreatedDate { get; set; }

    public List<string> Roles { get; set; } = [];
}
