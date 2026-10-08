using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;

/// <summary>
/// PKCE (RFC 7636). El cliente envía el hash de un secreto al pedir el código y el secreto al canjearlo:
/// quien intercepte el código no puede canjearlo sin ese secreto. Solo se admite <see cref="S256"/>.
/// </summary>
public static class Pkce
{
    public const string S256 = "S256";

    /// <summary>Un <c>code_verifier</c>: de 43 a 128 caracteres no reservados de URL.</summary>
    public const string CodeVerifierPattern = "^[A-Za-z0-9._~-]{43,128}$";

    /// <summary>Un <c>code_challenge</c> S256: el SHA-256 del verifier en base64url, sin relleno.</summary>
    public const string CodeChallengePattern = "^[A-Za-z0-9_-]{43}$";

    /// <summary>Indica si <c>BASE64URL(SHA256(codeVerifier)) == codeChallenge</c>, comparando en tiempo constante.</summary>
    public static bool IsValid(string codeVerifier, string codeChallenge)
    {
        var expected = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(codeChallenge));
    }
}
