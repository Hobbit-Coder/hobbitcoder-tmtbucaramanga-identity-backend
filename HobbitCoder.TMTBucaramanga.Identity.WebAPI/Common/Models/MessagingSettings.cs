namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// Pasarela HTTP de SMS/WhatsApp (sección <see cref="SectionName"/>). El proveedor no está contratado: la
/// pasarela recibe un mensaje con plantilla y parámetros y lo entrega por el canal pedido. Sin
/// <see cref="GatewayUrl"/>, en desarrollo el código se escribe en el log y en los demás ambientes la
/// verificación responde 503.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    /// <summary>URL absoluta https del endpoint que recibe los mensajes.</summary>
    public string? GatewayUrl { get; set; }

    /// <summary>Credencial de la pasarela (cabecera <c>Authorization: Bearer</c>). Solo por secretos.</summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 10;
}
