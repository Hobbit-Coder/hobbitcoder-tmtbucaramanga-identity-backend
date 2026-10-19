namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>WhatsApp Cloud API de Meta (requiere un negocio verificado para la plantilla de autenticación).</summary>
public class MetaWhatsAppSettings
{
    /// <summary>Raíz https de la Graph API.</summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>Versión de la Graph API (Meta retira cada versión unos dos años después de publicarla).</summary>
    public string ApiVersion { get; set; } = "v24.0";

    /// <summary>Identificador del número de WhatsApp Business que envía los mensajes.</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Token de acceso de un usuario del sistema de Meta. Solo por secretos.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Plantilla aprobada de categoría "Autenticación" con botón "Copiar código".</summary>
    public string CodeTemplate { get; set; } = "tmt_codigo_seguridad";

    /// <summary>Idioma con el que se aprobó la plantilla.</summary>
    public string TemplateLanguage { get; set; } = "es";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
