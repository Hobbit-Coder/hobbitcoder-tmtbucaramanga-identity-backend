using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Envía los mensajes de seguridad (§6, §23) al teléfono del titular de la cuenta. Es el único uso de
/// SMS/WhatsApp en Identity: no hay mensajes libres ni otros destinatarios. Nunca se envían contraseñas.
/// </summary>
public interface ISecurityMessageSender
{
    /// <exception cref="Exceptions.ExternalServiceException">El canal no está configurado o falló el envío.</exception>
    Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken);
}
