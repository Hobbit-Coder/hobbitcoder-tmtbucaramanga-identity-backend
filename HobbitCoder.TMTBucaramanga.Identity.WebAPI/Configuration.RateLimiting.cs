using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI;

public static partial class Configuration
{
    /// <summary>Política de los endpoints públicos que comprueban contraseñas o envían códigos.</summary>
    public const string PublicSecurityRateLimit = "public-security";

    /// <summary>
    /// Limita por IP los intentos de ingreso y de verificación: sin límite, cualquiera podría enviar códigos sin
    /// parar al teléfono de una cuenta o bloquearla probando contraseñas. Detrás de Traefik la IP real llega por
    /// <c>X-Forwarded-For</c> (<c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c> en el Dockerfile).
    /// </summary>
    public static IHostApplicationBuilder AddRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PublicSecurityRateLimit, context => context.Connection.RemoteIpAddress is { } ip
                ? RateLimitPartition.GetFixedWindowLimiter(ip.ToString(), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1)
                })
                // Sin IP (servidor de pruebas en memoria) no hay a quién limitar.
                : RateLimitPartition.GetNoLimiter("sin-ip"));
        });

        return builder;
    }
}
