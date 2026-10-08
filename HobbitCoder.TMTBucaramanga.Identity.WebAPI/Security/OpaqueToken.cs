using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>
/// Valores opacos (refresh tokens y códigos de autorización): 256 bits aleatorios en base64url. Se
/// guardan como SHA-256 sin sal, que basta porque el valor ya es aleatorio y largo (no es una contraseña
/// que se pueda adivinar por diccionario) y permite buscarlo por su hash.
/// </summary>
public static class OpaqueToken
{
    private const int SizeInBytes = 32;

    public static string Create() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(SizeInBytes));

    public static string ComputeHash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
