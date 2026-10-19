namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Genera los códigos de autorización opacos y calcula el hash con el que se guardan. El valor en claro
/// solo viaja al cliente; la base de datos guarda únicamente el hash.
/// </summary>
public interface IAuthorizationCodeGenerator
{
    /// <summary>Tiempo que el cliente tiene para canjear el código.</summary>
    TimeSpan Lifetime { get; }

    GeneratedAuthorizationCode Generate();

    string ComputeHash(string code);
}

public sealed class GeneratedAuthorizationCode
{
    /// <summary>Valor opaco que recibe el cliente.</summary>
    public required string Code { get; set; }

    /// <summary>Hash que se persiste.</summary>
    public required string CodeHash { get; set; }
}
