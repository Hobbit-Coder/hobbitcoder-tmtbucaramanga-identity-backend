using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>Códigos numéricos aleatorios y tokens opacos (<see cref="OpaqueToken"/>) de verificación.</summary>
public class VerificationSecrets : IVerificationSecrets
{
    private readonly VerificationSettings _settings;

    public VerificationSecrets(IOptions<VerificationSettings> options)
    {
        _settings = options.Value;
    }

    public TimeSpan Lifetime => TimeSpan.FromMinutes(_settings.LifetimeMinutes);

    public int MaxAttempts => _settings.MaxAttempts;

    public string CreateCode() =>
        string.Concat(Enumerable.Range(0, _settings.CodeLength).Select(_ => RandomNumberGenerator.GetInt32(10)));

    public string CreateToken() => OpaqueToken.Create();

    public string ComputeHash(string value) => OpaqueToken.ComputeHash(value);
}
