using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    /// <summary>
    /// Registra la verificación de identidad: política de códigos (<see cref="VerificationSettings"/>) y el
    /// envío de mensajes de seguridad por WhatsApp Cloud API según <see cref="MessagingSettings"/>.
    /// </summary>
    public static IHostApplicationBuilder AddSecurityMessaging(this IHostApplicationBuilder builder)
    {
        const string verification = VerificationSettings.SectionName;

        builder.Services.AddOptions<VerificationSettings>()
            .Bind(builder.Configuration.GetSection(verification))
            .Validate(settings => settings.CodeLength is >= 4 and <= 10, $"{verification}:CodeLength debe estar entre 4 y 10.")
            .Validate(settings => settings.LifetimeMinutes > 0, $"{verification}:LifetimeMinutes debe ser mayor que cero.")
            .Validate(settings => settings.MaxAttempts > 0, $"{verification}:MaxAttempts debe ser mayor que cero.")
            .ValidateOnStart();
        builder.Services.AddSingleton<IVerificationSecrets, VerificationSecrets>();

        builder.Services.AddOptions<MessagingSettings>()
            .Bind(builder.Configuration.GetSection(MessagingSettings.SectionName))
            .Validate(settings => IsHttpsUrl(settings.BaseUrl), $"{MessagingSettings.SectionName}:BaseUrl debe ser una URL absoluta https.")
            .Validate(settings => settings.TimeoutSeconds > 0, $"{MessagingSettings.SectionName}:TimeoutSeconds debe ser mayor que cero.")
            .ValidateOnStart();

        var settings = builder.Configuration.GetSection(MessagingSettings.SectionName).Get<MessagingSettings>() ?? new MessagingSettings();

        if (settings.IsConfigured)
        {
            builder.Services.AddHttpClient(WhatsAppSecurityMessageSender.HttpClientName,
                client => client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds));
            builder.Services.AddScoped<ISecurityMessageSender, WhatsAppSecurityMessageSender>();
        }
        else if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddScoped<ISecurityMessageSender, DevelopmentSecurityMessageSender>();
        }
        else
        {
            builder.Services.AddScoped<ISecurityMessageSender, UnavailableSecurityMessageSender>();
        }

        return builder;
    }

    private static bool IsHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
