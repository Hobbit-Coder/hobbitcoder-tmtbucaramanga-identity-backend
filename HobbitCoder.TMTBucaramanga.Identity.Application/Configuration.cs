using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Behaviors;
using HobbitCoder.TMTBucaramanga.Identity.Mediator;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace HobbitCoder.TMTBucaramanga.Identity.Application;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediator(Assembly.GetExecutingAssembly());

        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<AuthSessionService>();

        return builder;
    }
}
