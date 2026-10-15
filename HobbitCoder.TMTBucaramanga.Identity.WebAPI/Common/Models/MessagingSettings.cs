namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>Proveedor de WhatsApp para los códigos de seguridad.</summary>
public enum MessagingProvider
{
    /// <summary>Evolution API autoalojado: texto libre desde un número vinculado por QR.</summary>
    Evolution = 1,

    /// <summary>WhatsApp Cloud API de Meta: plantilla de autenticación aprobada.</summary>
    Meta = 2
}

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

/// <summary>Evolution API autoalojado (v2).</summary>
public class EvolutionSettings
{
    /// <summary>URL base de Evolution API, por ejemplo <c>https://evolution.hobbitcoder.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Token de la instancia (o la clave global) para la cabecera <c>apikey</c>. Solo por secretos.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Nombre de la instancia vinculada al número.</summary>
    public string? Instance { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Instance);
}

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
