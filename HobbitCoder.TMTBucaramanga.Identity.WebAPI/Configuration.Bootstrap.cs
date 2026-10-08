using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Services;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    /// <summary>Crea el primer administrador al arrancar si <see cref="BootstrapSettings"/> lo pide.</summary>
    public static IHostApplicationBuilder AddAdminBootstrap(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<BootstrapSettings>()
            .Bind(builder.Configuration.GetSection(BootstrapSettings.SectionName));

        builder.Services.AddHostedService<AdminBootstrapper>();

        return builder;
    }
}
