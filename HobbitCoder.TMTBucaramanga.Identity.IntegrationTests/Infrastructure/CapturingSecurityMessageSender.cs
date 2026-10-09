using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using System.Collections.Concurrent;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Doble de prueba de la pasarela de SMS/WhatsApp: guarda los mensajes de seguridad en memoria para que las
/// pruebas lean el código enviado. Solo existe en el proyecto de pruebas.
/// </summary>
public sealed class CapturingSecurityMessageSender : ISecurityMessageSender
{
    private readonly ConcurrentQueue<SentSecurityMessage> _messages = new();

    public IReadOnlyCollection<SentSecurityMessage> Messages => _messages;

    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        _messages.Enqueue(new SentSecurityMessage(phoneNumber, channel, purpose, code));
        return Task.CompletedTask;
    }

    /// <summary>Último código enviado a ese teléfono.</summary>
    public SentSecurityMessage LastTo(string phoneNumber) =>
        _messages.Last(message => message.PhoneNumber == phoneNumber);
}

public sealed record SentSecurityMessage(string PhoneNumber, VerificationChannel Channel, VerificationPurpose Purpose, string Code);
