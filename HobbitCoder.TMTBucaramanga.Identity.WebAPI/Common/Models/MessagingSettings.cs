namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// Envío de los códigos de seguridad por WhatsApp (sección <see cref="SectionName"/>). Sin las credenciales del
/// <see cref="Provider"/> elegido, en desarrollo el código se escribe en el log y en los demás ambientes la
/// verificación responde 503. Ningún proveedor envía SMS.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    public MessagingProvider Provider { get; set; } = MessagingProvider.Evolution;

    public int TimeoutSeconds { get; set; } = 10;

    public EvolutionSettings Evolution { get; set; } = new();

    public MetaWhatsAppSettings Meta { get; set; } = new();

    public bool IsConfigured => Provider switch
    {
        MessagingProvider.Evolution => Evolution.IsConfigured,
        MessagingProvider.Meta => Meta.IsConfigured,
        _ => false
    };
}
