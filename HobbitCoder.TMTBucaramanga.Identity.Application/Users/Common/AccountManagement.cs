using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>Reglas de quién puede administrar qué cuenta.</summary>
public static class AccountManagement
{
    /// <summary>
    /// Director y administrador administran cuentas (§4.1, §4.5). El rol Administrador está fuera de la
    /// jerarquía ministerial: solo otro administrador lo asigna o administra esas cuentas.
    /// </summary>
    /// <exception cref="ForbiddenAccessException">El actor no puede administrar cuentas con ese rol.</exception>
    public static void EnsureCanManageRole(this ICurrentUserService currentUserService, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(currentUserService);

        if (role == UserRole.Admin && !currentUserService.IsInRole(RoleNames.Admin))
        {
            throw new ForbiddenAccessException("Only an administrator can manage administrator accounts.");
        }
    }

    /// <summary>Nadie se desactiva, se retira ni cambia su propio rol: podría dejar la plataforma sin administración.</summary>
    /// <exception cref="ConflictException">La acción es sobre la propia cuenta.</exception>
    public static void EnsureNotSelf(this ICurrentUserService currentUserService, User user, string action)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == currentUserService.GetRequiredUserId())
        {
            throw new ConflictException($"You cannot {action} your own account.");
        }
    }

    /// <summary>
    /// Asigna los identificadores de acceso con sus formas normalizadas (las que usan los índices únicos y
    /// las búsquedas). Se hace aquí, y no con <c>UserManager.UpdateAsync</c>, para guardar la cuenta y su
    /// auditoría en un mismo <c>SaveChanges</c>.
    /// </summary>
    public static void SetIdentifiers(this User user, ILookupNormalizer normalizer, string userName, string phoneNumber, string? email)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizer);

        user.UserName = userName.Trim();
        user.NormalizedUserName = normalizer.NormalizeName(user.UserName);
        user.PhoneNumber = phoneNumber;
        user.Email = email;
        user.NormalizedEmail = email is null ? null : normalizer.NormalizeEmail(email);
    }

    /// <summary>
    /// Invalida la contraseña y el security stamp: sin contraseña la cuenta no puede ingresar hasta que su
    /// titular cree una nueva confirmando su identidad (§6).
    /// </summary>
    public static void ClearPassword(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.PasswordHash = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
    }
}
