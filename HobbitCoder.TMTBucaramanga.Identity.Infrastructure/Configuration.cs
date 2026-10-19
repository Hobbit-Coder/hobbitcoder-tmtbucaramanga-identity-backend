using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure;

public static class Configuration
{
    /// <summary>
    /// Registra los servicios de infraestructura comunes a todos los proveedores de base de datos.
    /// </summary>
    /// <remarks>
    /// <see cref="IApplicationDbContext"/> se resuelve desde la instancia de <see cref="IdentityContext"/>
    /// registrada por <c>AddDbContext</c>, para que en un mismo scope ambos compartan el ChangeTracker.
    /// <para>
    /// ASP.NET Core Identity se registra sin cookies, SignInManager ni roles (<c>AddIdentityCore</c>): la
    /// autenticación de la plataforma es con JWT y el rol es un atributo de la cuenta. La unicidad del correo
    /// (opcional) y del teléfono la comprueban los comandos y los índices únicos de la base.
    /// </para>
    /// <para>
    ///     RequireUniqueEmail exigiría correo a todas las cuentas; en TMT es opcional.
    /// </para>
    /// </remarks>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<IdentityContext>());
        builder.Services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        builder.Services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = false;

                options.Password.RequiredLength = 8;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<IdentityContext>();

        return builder;
    }
}
