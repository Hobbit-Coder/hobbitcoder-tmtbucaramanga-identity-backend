using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL
{
    public static class Configuration
    {
        public static IHostApplicationBuilder AddSqlServerPersistence(this IHostApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
                .AddCheck<SqlServerHealthCheck>("SQL Server", HealthStatus.Unhealthy);

            builder.AddInfrastructure();

            builder.Services.AddDbContext<SheepfoldContext>((serviceProvider, options) =>
                options.UseSqlServer(builder.Configuration["TmtBucaramangaSheepfoldSqlServerConnectionString"], sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(Configuration).Assembly.FullName);
                    sqlServerOptions.MigrationsHistoryTable("_EFMigrationsHistory", "Sheepfold");
                    sqlServerOptions.EnableRetryOnFailure(3);
                })
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

            return builder;
        }
    }
}
