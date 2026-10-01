using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Behaviors;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Publishers;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application;

public static partial class Configuration
{
    public static IHostApplicationBuilder AddMediator(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMediator, Mediator>();

        // El orden de registro es el orden de ejecución: LoggingBehavior envuelve a todos.
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Ejecuta los handlers de una notificación en secuencia. Para ejecutarlos en paralelo
        // se reemplaza por TaskWhenAllPublisher (ver sus restricciones con el DbContext).
        builder.Services.AddSingleton<INotificationPublisher, ForeachAwaitPublisher>();

        // Los handlers se descubren por ensamblado, igual que los validadores de FluentValidation.
        // Un handler de notificación puede atender varias notificaciones: se registra una vez por cada una.
        var handlerContracts = new[] { typeof(IRequestHandler<,>), typeof(INotificationHandler<>) };

        var registrations = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(
                type => type.GetInterfaces().Where(contract =>
                    contract.IsGenericType && handlerContracts.Contains(contract.GetGenericTypeDefinition())),
                (type, contract) => (Service: contract, Implementation: type));

        foreach (var (service, implementation) in registrations)
        {
            builder.Services.AddScoped(service, implementation);
        }

        return builder;
    }
}
