using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public static class UserQueryExtensions
{
    /// <summary>Proyecta los usuarios a <see cref="UserResponse"/> con sus roles en una sola consulta.</summary>
    public static IQueryable<UserResponse> SelectUserResponse(this IQueryable<User> users, IApplicationDbContext dbContext) =>
        users.Select(user => new UserResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnd = user.LockoutEnd,
            CreatedDate = user.CreatedDate,
            Roles = dbContext.UserRoles
                .Where(userRole => userRole.UserId == user.Id)
                .Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
                .OrderBy(roleName => roleName)
                .ToList()
        });

    /// <summary>
    /// Indica si el email ya lo usa otro usuario, incluidos los eliminados lógicamente: el índice único
    /// de la base de datos también los cuenta, así que reutilizar su email fallaría al guardar.
    /// </summary>
    public static Task<bool> IsEmailTakenAsync(this IApplicationDbContext dbContext, string normalizedEmail, Guid? exceptUserId, CancellationToken cancellationToken) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => (user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedEmail)
                && user.Id != exceptUserId, cancellationToken);

    /// <summary>Indica si todos los roles existen (sin distinguir mayúsculas).</summary>
    public static async Task<bool> RolesExistAsync(this IApplicationDbContext dbContext, IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var normalizedNames = roleNames.Select(roleName => roleName.ToUpperInvariant()).Distinct().ToList();

        var existing = await dbContext.Roles.CountAsync(role => normalizedNames.Contains(role.NormalizedName!), cancellationToken);

        return existing == normalizedNames.Count;
    }
}
