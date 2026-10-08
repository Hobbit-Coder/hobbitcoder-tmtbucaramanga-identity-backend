using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;

public class IdentityContext : IdentityDbContext<User, Role, Guid>, IApplicationDbContext
{
    private const int AuditUserMaxLength = 256;

    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
    {
        AuthorizationCodes = Set<AuthorizationCode>();
        RefreshTokens = Set<RefreshToken>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(PersistenceConstants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);

        ApplyAuditableConventions(builder);
        ApplySoftDeleteQueryFilters(builder);

        base.OnModelCreating(builder);
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
