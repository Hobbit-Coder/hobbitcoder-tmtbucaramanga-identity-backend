namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Aplicaciones de servidor (por ejemplo, la API de Sheepfold) que consultan el directorio de cuentas con
/// credenciales propias (OAuth 2.0 client credentials).
/// </summary>
public interface IServiceClientRegistry
{
    /// <summary>Indica si el cliente existe y el secreto es correcto (comparación de tiempo constante).</summary>
    bool IsValid(string clientId, string clientSecret);
}
