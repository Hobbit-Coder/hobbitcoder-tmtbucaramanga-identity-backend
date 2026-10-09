using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Handlers;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Utils;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddWebAPI(this IHostApplicationBuilder builder)
    {
        builder.AddCorsSettings();
        builder.AddJwtSettings();
        builder.AddSecurityMessaging();
        builder.AddAdminBootstrap();

        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return builder;
    }
}
