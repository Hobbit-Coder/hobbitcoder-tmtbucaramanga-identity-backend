using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Seeding;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Services;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    /// <summary>
    /// Crea el primer administrador al arrancar si <see cref="BootstrapSettings"/> lo pide y, después, las cuentas de
    /// la carga inicial si <c>Seeding:Enabled</c> está activo. El orden importa: el administrador nunca choca con ellas.
    /// </summary>
    public static IHostApplicationBuilder AddAdminBootstrap(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<BootstrapSettings>()
            .Bind(builder.Configuration.GetSection(BootstrapSettings.SectionName));

        builder.Services.AddHostedService<AdminBootstrapper>();
        builder.Services.AddHostedService<IdentitySeeder>();

        return builder;
    }
}
