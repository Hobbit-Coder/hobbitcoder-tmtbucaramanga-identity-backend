using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

/// <summary>Fuera de desarrollo y sin WhatsApp configurado: la verificación responde 503 en lugar de fingir el envío.</summary>
public class UnavailableSecurityMessageSender : ISecurityMessageSender
{
    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken) =>
        throw new ExternalServiceException("WhatsApp is not configured.");
}
