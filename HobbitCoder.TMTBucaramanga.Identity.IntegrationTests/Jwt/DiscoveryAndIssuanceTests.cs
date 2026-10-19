using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Jwt;

public class DiscoveryAndIssuanceTests : IClassFixture<IdentityApiFactory>
{
    private const string DiscoveryPath = "/.well-known/openid-configuration";
    private const string JwksPath = "/.well-known/jwks.json";

    private readonly IdentityApiFactory _factory;

    public DiscoveryAndIssuanceTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Discovery_PublishesConfiguredIssuerAndAbsoluteJwksUri()
    {
        var discovery = await GetJsonAsync(_factory.CreateIssuerClient(), DiscoveryPath);

        Assert.Equal(IdentityApiFactory.Issuer, discovery.GetProperty("issuer").GetString());
        Assert.Equal(IdentityApiFactory.Issuer + JwksPath, discovery.GetProperty("jwks_uri").GetString());
        Assert.Equal(["RS256"], discovery.GetProperty("id_token_signing_alg_values_supported").EnumerateArray().Select(alg => alg.GetString()));
    }

    [Fact]
    public async Task Discovery_DoesNotDeriveIssuerFromRequestHost()
    {
        using var client = _factory.CreateClient(new() { BaseAddress = new Uri("http://balanceador.interno:8080") });

        var discovery = await GetJsonAsync(client, DiscoveryPath);

        Assert.Equal(IdentityApiFactory.Issuer, discovery.GetProperty("issuer").GetString());
        Assert.Equal(IdentityApiFactory.Issuer + JwksPath, discovery.GetProperty("jwks_uri").GetString());
    }

    [Fact]
    public async Task Jwks_PublishesOnlyPublicRsaParameters()
    {
        var jwks = await GetJsonAsync(_factory.CreateIssuerClient(), JwksPath);
        var keys = jwks.GetProperty("keys").EnumerateArray().ToList();

        var key = Assert.Single(keys);
        Assert.Equal(["alg", "e", "kid", "kty", "n", "use"], key.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("sig", key.GetProperty("use").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
        Assert.False(string.IsNullOrWhiteSpace(key.GetProperty("kid").GetString()));
        Assert.True(Base64UrlEncoder.DecodeBytes(key.GetProperty("n").GetString()).Length * 8 >= 2048);

        foreach (var privateParameter in new[] { "d", "p", "q", "dp", "dq", "qi" })
        {
            Assert.False(key.TryGetProperty(privateParameter, out _), $"El JWKS expone el parámetro privado '{privateParameter}'.");
        }
    }

    [Fact]
    public async Task Jwks_DefaultKidIsRfc7638Thumbprint()
    {
        var jwks = await GetJsonAsync(_factory.CreateIssuerClient(), JwksPath);
        var kid = jwks.GetProperty("keys")[0].GetProperty("kid").GetString();

        using var rsa = RSA.Create();
        rsa.ImportFromPem(TestKeys.Active.PublicKeyPem);
        var parameters = rsa.ExportParameters(includePrivateParameters: false);
        var jwk = new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };

        Assert.Equal(Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint()), kid);
    }

    [Fact]
    public async Task IssuedToken_HasRs256HeaderAndContractClaims()
    {
        var discovery = await GetJsonAsync(_factory.CreateIssuerClient(), DiscoveryPath);
        var jwks = await GetJsonAsync(_factory.CreateIssuerClient(), JwksPath);
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();

        var accessToken = issuer.Issue(new AccessTokenRequest { Subject = "usuario-123", Audience = IdentityApiFactory.SheepfoldAudience, Roles = ["Admin"] });
        var token = new JsonWebToken(accessToken.Token);

        Assert.Equal("RS256", token.Alg);
        Assert.Equal("JWT", token.Typ);
        Assert.Equal(jwks.GetProperty("keys")[0].GetProperty("kid").GetString(), token.Kid);

        Assert.Equal(discovery.GetProperty("issuer").GetString(), token.Issuer);
        Assert.Equal(IdentityApiFactory.Issuer, token.Issuer);
        Assert.Equal([IdentityApiFactory.SheepfoldAudience], token.Audiences);
        Assert.Equal("usuario-123", token.Subject);
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
        Assert.Equal(["Admin"], token.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value));

        Assert.Equal(token.IssuedAt, token.ValidFrom);
        Assert.Equal(TimeSpan.FromMinutes(60), token.ValidTo - token.IssuedAt);
        Assert.Equal(accessToken.ExpiresAt.UtcDateTime, token.ValidTo);
    }

    [Fact]
    public void IssuedTokens_HaveUniqueJti()
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();
        var request = new AccessTokenRequest { Subject = "usuario-123", Audience = IdentityApiFactory.SheepfoldAudience, Roles = [] };

        var first = new JsonWebToken(issuer.Issue(request).Token);
        var second = new JsonWebToken(issuer.Issue(request).Token);

        Assert.NotEqual(first.Id, second.Id);
        Assert.DoesNotContain(first.Claims, claim => claim.Type == "role");
    }

    [Fact]
    public void Issue_RejectsUnregisteredAudience()
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();

        Assert.Throws<ArgumentException>(() =>
            issuer.Issue(new AccessTokenRequest { Subject = "usuario-123", Audience = "HobbitCoder.TMTBucaramanga.NoRegistrada", Roles = [] }));
    }

    [Fact]
    public void Issue_RejectsEmptySubject()
    {
        var issuer = _factory.Services.GetRequiredService<IAccessTokenIssuer>();

        Assert.Throws<ArgumentException>(() =>
            issuer.Issue(new AccessTokenRequest { Subject = " ", Audience = IdentityApiFactory.SheepfoldAudience, Roles = [] }));
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string path)
    {
        using (client)
        {
            using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return JsonDocument.Parse(content).RootElement.Clone();
        }
    }
}
