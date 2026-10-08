using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Jwt;

/// <summary>La aplicación no arranca sin un issuer válido ni sin una clave RSA privada válida.</summary>
public class StartupValidationTests
{
    public static TheoryData<string, string, string> InvalidSettings()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return new()
        {
            { "Jwt:SigningKey:Pem", string.Empty, "Jwt:SigningKey: no está configurada" },
            { "Jwt:SigningKey:Pem", "no-es-un-pem", "Jwt:SigningKey: no es una clave RSA válida" },
            { "Jwt:SigningKey:Pem", ecdsa.ExportPkcs8PrivateKeyPem(), "Jwt:SigningKey: no es una clave RSA válida" },
            { "Jwt:SigningKey:Pem", TestRsaKey.Create(1024).PrivateKeyPem, "el mínimo es 2048" },
            { "Jwt:SigningKey:Pem", TestKeys.Active.PublicKeyPem, "solo contiene la clave pública" },
            { "Jwt:SigningKey:PemPath", Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem"), "indica Pem o PemPath, no ambos" },
            { "Jwt:Issuer", string.Empty, "Jwt:Issuer es obligatorio" },
            { "Jwt:Issuer", "identity.tmtbucaramanga.test", "debe ser una URL absoluta" },
            { "Jwt:Issuer", IdentityApiFactory.Issuer + "/", "no debe terminar en '/'" },
            { "Jwt:Issuer", IdentityApiFactory.Issuer + "?tenant=1", "no debe tener query" },
            { "Jwt:Issuer", "http://identity.tmtbucaramanga.test", "debe usar https fuera de desarrollo" },
            { "Jwt:Audiences:0", string.Empty, "Jwt:Audiences" },
            { "Jwt:AccessTokenLifetimeMinutes", "0", "Jwt:AccessTokenLifetimeMinutes" },
            { "CorsSettings:AllowedOrigins:0", "*", "no admite '*'" },
            { "CorsSettings:AllowedOrigins:0", "https://sheepfold.tmtbucaramanga.com/", "sin barra final" },
            { "CorsSettings:AllowedOrigins:0", "https://sheepfold.tmtbucaramanga.com/app", "sin barra final" },
            { "CorsSettings:AllowedOrigins:0", "sheepfold.tmtbucaramanga.com", "debe ser una URL absoluta" },
            { "CorsSettings:AllowedOrigins:0", "http://sheepfold.tmtbucaramanga.com", "debe usar https fuera de desarrollo" }
        };
    }

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void App_DoesNotStart_WithInvalidJwtSettings(string key, string value, string expectedError)
    {
        using var factory = new IdentityApiFactory();
        factory.Settings[key] = value;

        var error = AssertDoesNotStart(factory);

        Assert.Contains(expectedError, error);
    }

    [Fact]
    public void App_DoesNotStart_WhenPemPathDoesNotExist()
    {
        using var factory = new IdentityApiFactory();
        factory.Settings["Jwt:SigningKey:Pem"] = string.Empty;
        factory.Settings["Jwt:SigningKey:PemPath"] = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");

        var error = AssertDoesNotStart(factory);

        Assert.Contains("no existe el archivo", error);
    }

    [Fact]
    public void App_DoesNotStart_WhenTwoPublishedKeysShareKid()
    {
        using var factory = new IdentityApiFactory();
        factory.Settings["Jwt:SigningKey:KeyId"] = "misma-clave";
        factory.Settings["Jwt:PreviousSigningKeys:0:KeyId"] = "misma-clave";
        factory.Settings["Jwt:PreviousSigningKeys:0:Pem"] = TestKeys.Previous.PublicKeyPem;

        var error = AssertDoesNotStart(factory);

        Assert.Contains("El kid 'misma-clave' está repetido", error);
    }

    [Fact]
    public async Task App_Starts_WithKeyFromPemPath()
    {
        var pemPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");
        await File.WriteAllTextAsync(pemPath, TestKeys.Active.PrivateKeyPem, TestContext.Current.CancellationToken);

        try
        {
            using var factory = new IdentityApiFactory();
            factory.Settings["Jwt:SigningKey:Pem"] = string.Empty;
            factory.Settings["Jwt:SigningKey:PemPath"] = pemPath;
            using var client = factory.CreateIssuerClient();

            var jwks = await client.GetFromJsonAsync<JsonElement>("/.well-known/jwks.json", TestContext.Current.CancellationToken);

            Assert.Equal(1, jwks.GetProperty("keys").GetArrayLength());
        }
        finally
        {
            File.Delete(pemPath);
        }
    }

    [Fact]
    public async Task App_Starts_WithHttpIssuerInDevelopment()
    {
        const string developmentIssuer = "http://localhost:5101";
        using var factory = new IdentityApiFactory { EnvironmentName = Environments.Development };
        factory.Settings["Jwt:Issuer"] = developmentIssuer;
        using var client = factory.CreateClient();

        var discovery = await client.GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration", TestContext.Current.CancellationToken);

        Assert.Equal(developmentIssuer, discovery.GetProperty("issuer").GetString());
    }

    private static string AssertDoesNotStart(IdentityApiFactory factory)
    {
        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient().Dispose());

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OptionsValidationException validationException)
            {
                return validationException.Message;
            }
        }

        Assert.Fail($"Se esperaba OptionsValidationException y se obtuvo: {exception}");
        return string.Empty;
    }
}
