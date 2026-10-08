using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>Claves generadas para las pruebas; ninguna se guarda en disco ni en el repositorio.</summary>
public static class TestKeys
{
    private static readonly Lazy<TestRsaKey> ActiveKey = new(() => TestRsaKey.Create(2048));
    private static readonly Lazy<TestRsaKey> PreviousKey = new(() => TestRsaKey.Create(2048));

    /// <summary>Clave que Identity usa para firmar en las pruebas.</summary>
    public static TestRsaKey Active => ActiveKey.Value;

    /// <summary>Clave de una rotación anterior: Identity solo la publica.</summary>
    public static TestRsaKey Previous => PreviousKey.Value;
}

public sealed record TestRsaKey(string PrivateKeyPem, string PublicKeyPem)
{
    public static TestRsaKey Create(int keySizeInBits)
    {
        using var rsa = RSA.Create(keySizeInBits);

        return new TestRsaKey(rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    public SigningCredentials CreateSigningCredentials(string keyId)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(PrivateKeyPem);

        return new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = keyId }, SecurityAlgorithms.RsaSha256);
    }
}
