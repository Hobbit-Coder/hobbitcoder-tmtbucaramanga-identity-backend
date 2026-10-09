using HobbitCoder.TMTBucaramanga.Identity.Infrastructure;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace HobbitCoder.TMTBucaramanga.Identity.Persistence.MSSQL;

public static class Configuration
{
    public static IHostApplicationBuilder AddSqlServerPersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sqlserver", HealthStatus.Unhealthy);

        builder.AddInfrastructure();

        builder.Services.AddDbContext<IdentityContext>((serviceProvider, options) =>
            options.UseSqlServer(builder.Configuration.GetConnectionString(PersistenceConstants.ConnectionStringName), sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(Configuration).Assembly.FullName);
                sqlServerOptions.MigrationsHistoryTable(PersistenceConstants.MigrationsHistoryTable, PersistenceConstants.Schema);
                sqlServerOptions.EnableRetryOnFailure(3);
            })
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

        return builder;
    }
}
