using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Role> Roles { get; }
    DbSet<User> Users { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
