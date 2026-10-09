using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

/// <summary>
/// Cuenta de una persona que ingresa a la plataforma (§3.1 y §5). La crea la administración sin
/// contraseña: el titular confirma su identidad, crea su contraseña y completa su perfil en el primer
/// ingreso (<see cref="ActivatedAt"/>).
/// </summary>
/// <remarks>
/// Identificadores de acceso: <c>UserName</c>, <c>PhoneNumber</c> (normalizado) y, si se registró,
/// <c>Email</c>. Los tres son únicos. Sin contraseña (<c>PasswordHash</c> nulo) no se puede ingresar:
/// es el estado de una cuenta nueva o con reinicio solicitado.
/// </remarks>
public class User : IdentityUser<Guid>, ISoftDeleteableEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required UserRole Role { get; set; }
    public required bool IsActive { get; set; }

    /// <summary>Designado para registrar asistentes nuevos (§7). No amplía su alcance de edición.</summary>
    public bool CanRegisterAttendees { get; set; }

    /// <summary>Momento en que el titular completó su primer ingreso; antes no puede iniciar sesión.</summary>
    public DateTime? ActivatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string CreatedBy { get; set; } = Guid.Empty.ToString();
    public DateTime CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public virtual ICollection<AuthorizationCode> AuthorizationCodes { get; set; } = [];
    public virtual ICollection<VerificationRequest> VerificationRequests { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Años cumplidos en <paramref name="today"/>; <c>null</c> sin fecha de nacimiento.</summary>
    public int? AgeOn(DateOnly today) => BirthDate is { } birthDate ? CalculateAge(birthDate, today) : null;

    public static int CalculateAge(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        return birthDate.AddYears(age) > today ? age - 1 : age;
    }
}
