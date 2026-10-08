using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>
/// Carga las claves RSA configuradas en <see cref="SigningKeySettings"/>. Cualquier problema (sin PEM,
/// formato inválido, clave que no es RSA, menos de <see cref="MinimumKeySizeInBits"/> bits, falta la
/// parte privada) se informa con <see cref="InvalidOperationException"/>.
/// </summary>
public static class RsaSigningKeyLoader
{
    public const int MinimumKeySizeInBits = 2048;

    /// <summary>Carga la clave completa, con su parte privada, para firmar.</summary>
    public static RsaSecurityKey LoadPrivateKey(SigningKeySettings settings, string contentRootPath)
    {
        var rsa = ImportRsa(settings, contentRootPath);

        try
        {
            if (!HasPrivateKey(rsa))
            {
                throw new InvalidOperationException("el PEM solo contiene la clave pública; para firmar se necesita la clave privada.");
            }

            return new RsaSecurityKey(rsa) { KeyId = ResolveKeyId(settings, rsa) };
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Carga solo la parte pública, aunque el PEM traiga la privada: es lo único que se publica en el
    /// JWKS y lo único que hace falta para validar.
    /// </summary>
    public static RsaSecurityKey LoadPublicKey(SigningKeySettings settings, string contentRootPath)
    {
        using var rsa = ImportRsa(settings, contentRootPath);

        return ToPublicKey(rsa, ResolveKeyId(settings, rsa));
    }

    public static RsaSecurityKey ToPublicKey(RSA rsa, string keyId)
    {
        var publicKey = RSA.Create();
        publicKey.ImportParameters(rsa.ExportParameters(includePrivateParameters: false));

        return new RsaSecurityKey(publicKey) { KeyId = keyId };
    }

    private static RSA ImportRsa(SigningKeySettings settings, string contentRootPath)
    {
        var pem = ReadPem(settings, contentRootPath);
        var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"no es una clave RSA válida en formato PEM ({ex.Message}).", ex);
        }

        if (rsa.KeySize < MinimumKeySizeInBits)
        {
            var keySize = rsa.KeySize;
            rsa.Dispose();
            throw new InvalidOperationException($"la clave RSA tiene {keySize} bits; el mínimo es {MinimumKeySizeInBits}.");
        }

        return rsa;
    }

    private static string ReadPem(SigningKeySettings settings, string contentRootPath)
    {
        var hasPem = !string.IsNullOrWhiteSpace(settings.Pem);
        var hasPemPath = !string.IsNullOrWhiteSpace(settings.PemPath);

        if (hasPem == hasPemPath)
        {
            throw new InvalidOperationException(hasPem
                ? "indica Pem o PemPath, no ambos."
                : "no está configurada: indica Pem o PemPath.");
        }

        if (hasPem)
        {
            return settings.Pem!;
        }

        var path = Path.GetFullPath(settings.PemPath!, contentRootPath);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"no existe el archivo '{path}'.");
        }

        return File.ReadAllText(path);
    }

    private static bool HasPrivateKey(RSA rsa)
    {
        try
        {
            rsa.ExportParameters(includePrivateParameters: true);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>Usa el <c>kid</c> configurado o, si no hay, el thumbprint JWK de la clave (RFC 7638).</summary>
    private static string ResolveKeyId(SigningKeySettings settings, RSA rsa)
    {
        if (!string.IsNullOrWhiteSpace(settings.KeyId))
        {
            return settings.KeyId;
        }

        var parameters = rsa.ExportParameters(includePrivateParameters: false);
        var jwk = new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };

        return Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
    }
}
