using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>Códigos de autorización opacos (<see cref="OpaqueToken"/>) de vida muy corta.</summary>
public class AuthorizationCodeGenerator : IAuthorizationCodeGenerator
{
    /// <summary>El cliente lo canjea apenas vuelve del navegador: un minuto es suficiente.</summary>
    public TimeSpan Lifetime => TimeSpan.FromMinutes(1);

    public GeneratedAuthorizationCode Generate()
    {
        var code = OpaqueToken.Create();

        return new GeneratedAuthorizationCode(code, ComputeHash(code));
    }

    public string ComputeHash(string code) => OpaqueToken.ComputeHash(code);
}
