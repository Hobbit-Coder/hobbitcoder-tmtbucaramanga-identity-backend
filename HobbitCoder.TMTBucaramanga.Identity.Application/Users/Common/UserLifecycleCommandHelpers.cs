using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public static class UserLifecycleCommandHelpers
{
    public static async Task<User> FindManageableAsync(ICurrentUserService currentUserService, IQueryable<User> users, Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        currentUserService.EnsureCanManageRole(user.Role);

        return user;
    }

    /// <summary>Cuentas en registros retirados: eliminadas lógicamente o desactivadas.</summary>
    public static IQueryable<User> RetiredUsers(IApplicationDbContext dbContext) =>
        dbContext.Users.IgnoreQueryFilters().Where(user => user.IsDeleted || !user.IsActive);
}
