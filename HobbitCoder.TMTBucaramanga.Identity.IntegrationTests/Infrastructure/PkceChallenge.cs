using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>Par PKCE S256 como el que genera un cliente: un verifier aleatorio y su desafío.</summary>
public sealed class PkceChallenge
{
    public required string CodeVerifier { get; set; }
    public required string CodeChallenge { get; set; }

    public static PkceChallenge Create()
    {
        var codeVerifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var codeChallenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        return new PkceChallenge { CodeVerifier = codeVerifier, CodeChallenge = codeChallenge };
    }
}
