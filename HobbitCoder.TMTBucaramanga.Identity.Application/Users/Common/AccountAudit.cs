using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>
/// Registra en la auditoría de cuentas (§25) los cambios hechos sobre una cuenta. Se agrega al mismo
/// <c>SaveChanges</c> del cambio, así que ambos se guardan o fallan juntos.
/// </summary>
public static class AccountAudit
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Datos de la cuenta que se conservan en la auditoría. Nunca contraseñas, hashes ni tokens.</summary>
    public static AccountSnapshot Snapshot(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new AccountSnapshot(
            user.FirstName, user.LastName, user.Gender?.ToString(), user.PhoneNumber, user.Email, user.UserName,
            user.Instagram, user.BirthDate, user.Role.ToString(), user.IsActive, user.CanRegisterAttendees,
            UserResponse.GetAccessStatus(user).ToString());
    }

    public static void Record(
        this IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        AccountAuditAction action,
        User user,
        AccountSnapshot? before,
        AccountSnapshot? after)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(user);

        dbContext.AccountAuditEntries.Add(new AccountAuditEntry
        {
            OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId = currentUserService.UserId,
            Action = action,
            TargetUserId = user.Id,
            TargetName = user.FullName,
            Before = before is null ? null : JsonSerializer.Serialize(before, SerializerOptions),
            After = after is null ? null : JsonSerializer.Serialize(after, SerializerOptions)
        });
    }
}

public sealed record AccountSnapshot(
    string FirstName,
    string LastName,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? UserName,
    string? Instagram,
    DateOnly? BirthDate,
    string Role,
    bool IsActive,
    bool CanRegisterAttendees,
    string AccessStatus);
