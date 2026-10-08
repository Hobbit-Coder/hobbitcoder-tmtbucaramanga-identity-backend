using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>
/// Claves de firma cargadas de <see cref="JwtSettings"/>: la activa, que firma los tokens nuevos, y
/// todas las que se publican en el JWKS (la activa más <see cref="JwtSettings.PreviousSigningKeys"/>).
/// </summary>
/// <remarks>
/// Las claves viven lo mismo que la aplicación y no se liberan: <c>CryptoProviderFactory.Default</c>
/// guarda en caché los proveedores de firma por el thumbprint de la clave, y liberar el <c>RSA</c> dejaría
/// en esa caché un proveedor inservible para cualquier otro host del proceso con la misma clave.
/// </remarks>
public sealed class SigningKeyStore
{
    public SigningKeyStore(IOptions<JwtSettings> options, IHostEnvironment environment)
    {
        var settings = options.Value;

        var activeKey = RsaSigningKeyLoader.LoadPrivateKey(settings.SigningKey, environment.ContentRootPath);
        SigningCredentials = new SigningCredentials(activeKey, SecurityAlgorithms.RsaSha256);

        PublishedKeys =
        [
            RsaSigningKeyLoader.ToPublicKey(activeKey.Rsa, activeKey.KeyId),
            .. settings.PreviousSigningKeys.Select(key => RsaSigningKeyLoader.LoadPublicKey(key, environment.ContentRootPath))
        ];
    }

    /// <summary>Credenciales RS256 de la clave activa; su <c>kid</c> va en el header de cada token.</summary>
    public SigningCredentials SigningCredentials { get; }

    /// <summary>Claves públicas que se publican en el JWKS, cada una con su <c>kid</c>.</summary>
    public IReadOnlyList<RsaSecurityKey> PublishedKeys { get; }
}
