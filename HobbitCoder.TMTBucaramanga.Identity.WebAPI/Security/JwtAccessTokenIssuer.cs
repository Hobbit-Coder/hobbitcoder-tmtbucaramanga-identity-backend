using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>
/// Emite los tokens de acceso firmados con RS256 por la clave activa de <see cref="SigningKeyStore"/>.
/// El header lleva <c>alg</c>, <c>typ</c> y el <c>kid</c> de la clave; el claim <c>iss</c> es
/// exactamente <see cref="JwtSettings.Issuer"/>, el mismo valor que publica el discovery.
/// </summary>
public class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    /// <summary>Las APIs consumidoras lo reciben como <c>ClaimTypes.Role</c> (MapInboundClaims).</summary>
    public const string RoleClaimType = "role";

    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtSettings _settings;
    private readonly SigningKeyStore _signingKeyStore;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenIssuer(IOptions<JwtSettings> options, SigningKeyStore signingKeyStore, TimeProvider timeProvider)
    {
        _settings = options.Value;
        _signingKeyStore = signingKeyStore;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Emite un token de acceso firmado.
    /// </summary>
    /// <remarks>
    ///     exp, iat y nbf van en segundos: se trunca aquí para que ExpiresAt coincida con el claim exp.
    /// </remarks>
    public AccessToken Issue(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("El subject del token es obligatorio.", nameof(request));
        }

        if (!IsRegisteredAudience(request.Audience))
        {
            throw new ArgumentException(
                $"La audiencia '{request.Audience}' no está registrada en {JwtSettings.SectionName}:Audiences.", nameof(request));
        }

        var now = _timeProvider.GetUtcNow();
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds());
        var expiresAt = issuedAt.AddMinutes(_settings.AccessTokenLifetimeMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = request.Subject,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            claims[JwtRegisteredClaimNames.Sid] = request.SessionId;
        }

        var roles = request.Roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.Ordinal).ToArray();

        if (roles.Length > 0)
        {
            claims[RoleClaimType] = roles;
        }

        var permissions = (request.Permissions ?? []).Where(permission => !string.IsNullOrWhiteSpace(permission)).Distinct(StringComparer.Ordinal).ToArray();

        if (permissions.Length > 0)
        {
            claims[PermissionNames.ClaimType] = permissions;
        }

        var token = TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = request.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = claims,
            TokenType = JwtConstants.HeaderType,
            SigningCredentials = _signingKeyStore.SigningCredentials
        });

        return new AccessToken { Token = token, ExpiresAt = expiresAt };
    }

    public bool IsRegisteredAudience(string audience) =>
        _settings.Audiences.Contains(audience, StringComparer.Ordinal);
}
