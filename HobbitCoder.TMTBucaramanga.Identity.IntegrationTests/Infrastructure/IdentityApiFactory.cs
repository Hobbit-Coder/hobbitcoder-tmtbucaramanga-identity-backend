using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta Identity en memoria con una configuración JWT válida. <see cref="Settings"/> y
/// <see cref="EnvironmentName"/> se pueden cambiar antes de usar el factory para probar otras configuraciones.
/// </summary>
/// <remarks>
/// Fuera de <c>Development</c> no se cargan los user-secrets del equipo, así que solo cuenta la
/// configuración de la prueba.
/// </remarks>
public class IdentityApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://identity.tmtbucaramanga.test";
    public const string SheepfoldAudience = "HobbitCoder.TMTBucaramanga.Sheepfold";
    public const string OtherAudience = "HobbitCoder.TMTBucaramanga.OtraApi";
    public const string IdentityAudience = WebAPI.Configuration.IdentityAudience;
    public const string TestingEnvironment = "Testing";
    public const string SheepfoldClientId = "sheepfold-web";
    public const string SheepfoldRedirectUri = "https://sheepfold.tmtbucaramanga.test/auth/callback";

    public string EnvironmentName { get; init; } = TestingEnvironment;

    /// <summary>Reemplazos de servicios para la prueba (por ejemplo, la pasarela de mensajería).</summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audiences:0"] = SheepfoldAudience,
        ["Jwt:Audiences:1"] = OtherAudience,
        ["Jwt:Audiences:2"] = IdentityAudience,
        ["Jwt:Clients:0:ClientId"] = SheepfoldClientId,
        ["Jwt:Clients:0:Audience"] = SheepfoldAudience,
        ["Jwt:Clients:0:RedirectUris:0"] = SheepfoldRedirectUri,
        ["Jwt:SigningKey:Pem"] = TestKeys.Active.PrivateKeyPem,
        ["Jwt:SigningKey:PemPath"] = string.Empty,
        ["Jwt:SigningKey:KeyId"] = string.Empty
    };

    /// <summary>Cliente que llama a Identity por su URL pública, como lo hacen las APIs consumidoras.</summary>
    public HttpClient CreateIssuerClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(Issuer) });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Settings));

        if (ConfigureServices is not null)
        {
            builder.ConfigureTestServices(ConfigureServices);
        }
    }
}
