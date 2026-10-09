using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>Clientes de servicio de <c>Jwt:ServiceClients</c>, validados al arrancar por <see cref="JwtSettingsValidator"/>.</summary>
public class ConfiguredServiceClientRegistry : IServiceClientRegistry
{
    private readonly Dictionary<string, byte[]> _secretHashes;

    public ConfiguredServiceClientRegistry(IOptions<JwtSettings> options)
    {
        _secretHashes = options.Value.ServiceClients.ToDictionary(
            client => client.ClientId, client => Hash(client.Secret), StringComparer.Ordinal);
    }

    public bool IsValid(string clientId, string clientSecret) =>
        _secretHashes.TryGetValue(clientId, out var expected)
        && CryptographicOperations.FixedTimeEquals(expected, Hash(clientSecret));

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
