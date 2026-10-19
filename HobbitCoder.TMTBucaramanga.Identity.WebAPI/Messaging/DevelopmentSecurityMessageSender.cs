using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

/// <summary>
/// Solo en desarrollo y sin WhatsApp configurado: escribe el código en el log para probar el primer
/// ingreso y la recuperación sin enviar mensajes reales. Nunca se registra fuera de desarrollo.
/// </summary>
public class DevelopmentSecurityMessageSender : ISecurityMessageSender
{
    private readonly ILogger<DevelopmentSecurityMessageSender> _logger;

    public DevelopmentSecurityMessageSender(ILogger<DevelopmentSecurityMessageSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        _logger.LogWarning("DESARROLLO · Código de {Purpose} por {Channel} para {PhoneNumber}: {Code}", purpose, channel, phoneNumber, code);

        return Task.CompletedTask;
    }
}
