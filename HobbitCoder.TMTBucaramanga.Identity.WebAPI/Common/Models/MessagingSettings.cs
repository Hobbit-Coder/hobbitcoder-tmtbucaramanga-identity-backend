namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// WhatsApp Cloud API de Meta para los códigos de seguridad (sección <see cref="SectionName"/>). Sin
/// <see cref="PhoneNumberId"/> y <see cref="AccessToken"/>, en desarrollo el código se escribe en el log y en
/// los demás ambientes la verificación responde 503. El proveedor no envía SMS.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    /// <summary>Raíz https de la Graph API.</summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>Versión de la Graph API (Meta retira cada versión unos dos años después de publicarla).</summary>
    public string ApiVersion { get; set; } = "v24.0";

    /// <summary>Identificador del número de WhatsApp Business que envía los mensajes.</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Token de acceso permanente de un usuario del sistema de Meta. Solo por secretos.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Plantilla aprobada de categoría "Autenticación" con botón "Copiar código".</summary>
    public string CodeTemplate { get; set; } = "tmt_codigo_seguridad";

    /// <summary>Idioma con el que se aprobó la plantilla.</summary>
    public string TemplateLanguage { get; set; } = "es";

    public int TimeoutSeconds { get; set; } = 10;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
