using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Behaviors;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Los handlers se descubren por ensamblado, igual que los validadores de FluentValidation.
        builder.Services.AddMediator(Assembly.GetExecutingAssembly());

        // El orden de registro es el orden de ejecución: LoggingBehavior envuelve a todos.
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return builder;
    }
}
