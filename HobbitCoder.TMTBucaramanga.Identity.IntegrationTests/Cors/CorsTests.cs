using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Cors;

/// <summary>
/// CORS con la configuración real de cada ambiente (<c>appsettings.{Ambiente}.json</c>). En producción el
/// frontend vive en el mismo dominio que Sheepfold, que para Identity es otro origen.
/// </summary>
public class CorsTests
{
    private const string ProductionFrontend = "https://sheepfold.tmtbucaramanga.com";
    private const string DevelopmentFrontend = "http://localhost:4200";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Production_AllowsTheFrontendToLogIn()
    {
        using var factory = new IdentityApiFactory { EnvironmentName = Environments.Production };
        using var client = factory.CreateIssuerClient();

        using var response = await SendPreflightAsync(client, ProductionFrontend, "/api/auth/login", "POST");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ProductionFrontend, GetHeader(response, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", GetHeader(response, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", GetHeader(response, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("content-type", GetHeader(response, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("600", GetHeader(response, "Access-Control-Max-Age"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Theory]
    [InlineData(DevelopmentFrontend)]
    [InlineData("https://otro-sitio.example.com")]
    [InlineData("https://identity.tmtbucaramanga.com")]
    public async Task Production_RejectsOtherOrigins(string origin)
    {
        using var factory = new IdentityApiFactory { EnvironmentName = Environments.Production };
        using var client = factory.CreateIssuerClient();

        using var response = await SendPreflightAsync(client, origin, "/api/auth/login", "POST");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Production_ActualRequest_ExposesAllowedOriginAndHeaders()
    {
        using var factory = new IdentityApiFactory { EnvironmentName = Environments.Production };
        using var client = factory.CreateIssuerClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/openid-configuration");
        request.Headers.Add("Origin", ProductionFrontend);

        using var response = await client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ProductionFrontend, GetHeader(response, "Access-Control-Allow-Origin"));
        Assert.Contains("Location", GetHeader(response, "Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task Development_AllowsTheLocalFrontend()
    {
        using var factory = new IdentityApiFactory { EnvironmentName = Environments.Development };
        using var client = factory.CreateIssuerClient();

        using var response = await SendPreflightAsync(client, DevelopmentFrontend, "/api/users/01a0fd28-eb05-7fba-8260-b2345d93bcec", "DELETE");

        Assert.Equal(DevelopmentFrontend, GetHeader(response, "Access-Control-Allow-Origin"));
        Assert.Contains("DELETE", GetHeader(response, "Access-Control-Allow-Methods"));
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(HttpClient client, string origin, string uri, string method)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, uri);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        return await client.SendAsync(request, CancellationToken);
    }

    private static string GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : string.Empty;
}
