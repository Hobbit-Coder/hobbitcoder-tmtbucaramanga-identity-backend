using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>Clientes registrados en <c>Jwt:Clients</c>, validados al arrancar por <see cref="JwtSettingsValidator"/>.</summary>
public class ConfiguredClientRegistry : IClientRegistry
{
    private readonly Dictionary<string, RegisteredClient> _clients;

    public ConfiguredClientRegistry(IOptions<JwtSettings> options)
    {
        _clients = options.Value.Clients.ToDictionary(
            client => client.ClientId,
            client => new RegisteredClient { ClientId = client.ClientId, Audience = client.Audience, RedirectUris = [.. client.RedirectUris] },
            StringComparer.Ordinal);
    }

    public RegisteredClient? FindClient(string clientId) => _clients.GetValueOrDefault(clientId);
}
