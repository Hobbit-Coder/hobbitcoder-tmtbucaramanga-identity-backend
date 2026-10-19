namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Genera los refresh tokens opacos y calcula el hash con el que se guardan. El valor en claro solo
/// se entrega al cliente; la base de datos guarda únicamente el hash.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Vida de cada refresh token desde que se emite (<c>Jwt:RefreshTokenLifetimeDays</c>).</summary>
    TimeSpan Lifetime { get; }

    GeneratedRefreshToken Generate();

    string ComputeHash(string token);
}

public sealed class GeneratedRefreshToken
{
    /// <summary>Valor opaco que recibe el cliente.</summary>
    public required string Token { get; set; }

    /// <summary>Hash que se persiste.</summary>
    public required string TokenHash { get; set; }
}
