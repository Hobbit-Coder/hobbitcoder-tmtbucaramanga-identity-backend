using FluentValidation;
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
        builder.AddMediator();
        return builder;
    }
}
