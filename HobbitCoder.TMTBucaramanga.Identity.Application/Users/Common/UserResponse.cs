using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>Estado del acceso de la cuenta, derivado de su activación y de su contraseña.</summary>
public enum AccessStatus
{
    /// <summary>El titular todavía no completa el primer ingreso.</summary>
    PendingActivation = 1,

    /// <summary>Puede ingresar con su contraseña.</summary>
    Ready = 2,

    /// <summary>La administración pidió que cree una contraseña nueva.</summary>
    PasswordResetRequested = 3
}

public class UserResponse
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string UserName { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }

    /// <summary>Edad calculada con la fecha de nacimiento (§5); no se captura.</summary>
    public int? Age { get; set; }

    public required UserRole Role { get; set; }
    public required bool IsActive { get; set; }
    public required bool CanRegisterAttendees { get; set; }
    public required AccessStatus AccessStatus { get; set; }

    /// <summary>Fin del bloqueo por intentos fallidos de inicio de sesión; <c>null</c> si no está bloqueado.</summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    public required DateTime CreatedDate { get; set; }
    public DateTime? DeletedAt { get; set; }

    public static UserResponse From(User user, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Gender = user.Gender,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            UserName = user.UserName!,
            Instagram = user.Instagram,
            BirthDate = user.BirthDate,
            Age = user.AgeOn(today),
            Role = user.Role,
            IsActive = user.IsActive,
            CanRegisterAttendees = user.CanRegisterAttendees,
            AccessStatus = GetAccessStatus(user),
            LockoutEnd = user.LockoutEnd,
            CreatedDate = user.CreatedDate,
            DeletedAt = user.DeletedAt
        };
    }

    public static AccessStatus GetAccessStatus(User user) => user switch
    {
        { ActivatedAt: null } => AccessStatus.PendingActivation,
        { PasswordHash: null } => AccessStatus.PasswordResetRequested,
        _ => AccessStatus.Ready
    };
}
