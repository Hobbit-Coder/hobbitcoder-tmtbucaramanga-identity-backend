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

/// <param name="Code">Valor opaco que recibe el cliente.</param>
/// <param name="CodeHash">Hash que se persiste.</param>
public sealed record GeneratedAuthorizationCode(string Code, string CodeHash);
