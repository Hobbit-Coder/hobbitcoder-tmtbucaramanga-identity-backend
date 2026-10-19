using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>Refresh tokens opacos (<see cref="OpaqueToken"/>).</summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtSettings _settings;

    public RefreshTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TimeSpan Lifetime => TimeSpan.FromDays(_settings.RefreshTokenLifetimeDays);

    public GeneratedRefreshToken Generate()
    {
        var token = OpaqueToken.Create();

        return new GeneratedRefreshToken { Token = token, TokenHash = ComputeHash(token) };
    }

    public string ComputeHash(string token) => OpaqueToken.ComputeHash(token);
}
