namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;

/// <summary>Par de tokens de una sesión.</summary>
public class AuthResponse
{
    /// <summary>JWT RS256 para la API indicada en la audiencia.</summary>
    public required string AccessToken { get; set; }

    public string TokenType { get; set; } = "Bearer";

    /// <summary>Segundos de vida del token de acceso.</summary>
    public required int ExpiresIn { get; set; }

    public required DateTimeOffset AccessTokenExpiresAt { get; set; }

    /// <summary>Token opaco para renovar la sesión en <c>/api/auth/refresh</c>. Solo lo usa Identity.</summary>
    public required string RefreshToken { get; set; }

    public required DateTimeOffset RefreshTokenExpiresAt { get; set; }
}
