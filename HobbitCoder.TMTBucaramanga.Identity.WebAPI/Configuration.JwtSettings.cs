using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    /// <summary>Valor del claim <c>aud</c> que acepta la propia API de Identity.</summary>
    public const string IdentityAudience = "HobbitCoder.TMTBucaramanga.Identity";

    /// <summary>
    /// Registra la emisión de los JWT de la plataforma (<see cref="IAccessTokenIssuer"/>) y las claves
    /// que se publican en <c>/.well-known/jwks.json</c>. <see cref="JwtSettings"/> se valida al arrancar:
    /// sin un emisor válido o sin una clave RSA privada de al menos 2048 bits la aplicación no inicia.
    /// </summary>
    /// <remarks>
    /// Los endpoints propios de Identity que declaren <c>[Authorize]</c> validan los tokens con las mismas
    /// claves en memoria (sin descargar su propio discovery) y solo aceptan la audiencia
    /// <see cref="IdentityAudience"/>, para que un token emitido para otra API no sirva aquí.
    /// </remarks>
    public static IHostApplicationBuilder AddJwtSettings(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
            .ValidateOnStart();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<SigningKeyStore>();
        builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        builder.Services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        builder.Services.AddSingleton<IAuthorizationCodeGenerator, AuthorizationCodeGenerator>();
        builder.Services.AddSingleton<IClientRegistry, ConfiguredClientRegistry>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>, SigningKeyStore>((options, jwtSettings, signingKeyStore) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = IdentityAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeyStore.PublishedKeys,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
