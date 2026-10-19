namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>Proveedor de WhatsApp para los códigos de seguridad.</summary>
public enum MessagingProvider
{
    /// <summary>Evolution API autoalojado: texto libre desde un número vinculado por QR.</summary>
    Evolution = 1,

    /// <summary>WhatsApp Cloud API de Meta: plantilla de autenticación aprobada.</summary>
    Meta = 2
}
