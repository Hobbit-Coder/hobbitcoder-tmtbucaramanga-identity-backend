using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;

/// <remarks>
/// <see cref="IdentityUserContext{TUser, TKey}"/> sin tablas de roles: cada cuenta tiene un único rol, que es un
/// atributo de <see cref="User"/> (una tabla de asociación con una sola fila por usuario sería una relación 1:1).
/// </remarks>
public class IdentityContext : IdentityUserContext<User, Guid>, IApplicationDbContext
{
    private const int AuditUserMaxLength = 256;

    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<VerificationRequest> VerificationRequests { get; set; }
    public DbSet<AccountAuditEntry> AccountAuditEntries { get; set; }

    public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
    {
        AuthorizationCodes = Set<AuthorizationCode>();
        RefreshTokens = Set<RefreshToken>();
        VerificationRequests = Set<VerificationRequest>();
        AccountAuditEntries = Set<AccountAuditEntry>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Primero el modelo de ASP.NET Core Identity, para que las configuraciones propias lo ajusten.
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(PersistenceConstants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);

        ApplyAuditableConventions(builder);
        ApplySoftDeleteQueryFilters(builder);
    }

    private static void ApplyAuditableConventions(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<IAuditableEntity>(modelBuilder))
        {
            var entity = modelBuilder.Entity(clrType);

            entity.Property(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(AuditUserMaxLength);
            entity.Property(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(AuditUserMaxLength);

            if (typeof(ISoftDeleteableEntity).IsAssignableFrom(clrType))
            {
                entity.Property(nameof(ISoftDeleteableEntity.DeletedBy)).HasMaxLength(AuditUserMaxLength);
            }
        }
    }

    /// <summary>
    /// Oculta los registros borrados lógicamente en todas las consultas. Para verlos,
    /// una consulta debe pedirlo explícitamente con <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var clrType in GetEntityClrTypes<ISoftDeleteableEntity>(modelBuilder))
        {
            var entity = Expression.Parameter(clrType, "entity");
            var isNotDeleted = Expression.Not(Expression.Property(entity, nameof(ISoftDeleteableEntity.IsDeleted)));

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(isNotDeleted, entity));
        }
    }

    private static List<Type> GetEntityClrTypes<TContract>(ModelBuilder modelBuilder) =>
        modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(TContract).IsAssignableFrom(clrType))
            .ToList();
}
