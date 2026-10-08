using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Aplicaciones cliente (por ejemplo, el frontend de Sheepfold) autorizadas a iniciar sesión con
/// Identity mediante código de autorización.
/// </summary>
public interface IClientRegistry
{
    /// <summary>El cliente registrado con ese id, o <c>null</c> si no existe.</summary>
    RegisteredClient? FindClient(string clientId);
}
