using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<User> Users { get; }
    DbSet<VerificationRequest> VerificationRequests { get; }
    DbSet<AccountAuditEntry> AccountAuditEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
