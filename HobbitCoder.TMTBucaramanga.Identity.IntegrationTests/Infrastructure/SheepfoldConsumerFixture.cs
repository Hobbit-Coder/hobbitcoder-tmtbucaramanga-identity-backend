using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Identity y una API consumidora configurada exactamente como Sheepfold: <c>Authority</c> = issuer de
/// Identity, sin <c>ValidIssuer</c> (se usa el <c>issuer</c> del discovery) y claves del <c>jwks_uri</c>.
/// La única diferencia con producción es que el discovery y el JWKS se descargan del TestServer de Identity.
/// </summary>
/// <remarks>
/// Identity publica además <see cref="TestKeys.Previous"/> (solo su clave pública) con el kid
/// <see cref="PreviousKeyId"/>, como tras una rotación.
/// </remarks>
public sealed class SheepfoldConsumerFixture : IAsyncLifetime
{
    public const string PreviousKeyId = "clave-anterior";
    public const string WhoAmIPath = "/whoami";

    private WebApplication? _consumer;

    public SheepfoldConsumerFixture()
    {
        Identity.Settings["Jwt:PreviousSigningKeys:0:KeyId"] = PreviousKeyId;
        Identity.Settings["Jwt:PreviousSigningKeys:0:Pem"] = TestKeys.Previous.PublicKeyPem;
    }

    public IdentityApiFactory Identity { get; } = new();

    /// <summary>Cliente de la API consumidora.</summary>
    public HttpClient Consumer { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();

        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = IdentityApiFactory.Issuer;
                options.RequireHttpsMetadata = !isDevelopment;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = IdentityApiFactory.SheepfoldAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };

                options.BackchannelHttpHandler = Identity.Server.CreateHandler();
            });
        builder.Services.AddAuthorization();

        _consumer = builder.Build();
        _consumer.UseAuthentication();
        _consumer.UseAuthorization();
        _consumer.MapGet(WhoAmIPath, (ClaimsPrincipal user) => new WhoAmIResponse(
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                [.. user.FindAll(ClaimTypes.Role).Select(claim => claim.Value)]))
            .RequireAuthorization();

        await _consumer.StartAsync();
        Consumer = _consumer.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Consumer?.Dispose();

        if (_consumer is not null)
        {
            await _consumer.DisposeAsync();
        }

        await Identity.DisposeAsync();
    }
}

/// <summary>Lo que ve la API consumidora del usuario autenticado, con los claims ya mapeados.</summary>
public sealed record WhoAmIResponse(string? NameIdentifier, IReadOnlyList<string> Roles);
