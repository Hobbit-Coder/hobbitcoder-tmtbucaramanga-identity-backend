using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Jwt;

/// <summary>
/// Valida los tokens de Identity con la configuración de Sheepfold: discovery y JWKS descargados de
/// Identity con <c>Authority</c> = issuer configurado.
/// </summary>
public class SheepfoldTokenValidationTests : IClassFixture<SheepfoldConsumerFixture>
{
    private readonly SheepfoldConsumerFixture _fixture;

    public SheepfoldTokenValidationTests(SheepfoldConsumerFixture fixture)
    {
        _fixture = fixture;
    }

    private IServiceProvider IdentityServices => _fixture.Identity.Services;

    [Fact]
    public async Task TokenIssuedByIdentity_IsAcceptedAndClaimsAreMapped()
    {
        var issuer = IdentityServices.GetRequiredService<IAccessTokenIssuer>();
        var accessToken = issuer.Issue(new AccessTokenRequest { Subject = "usuario-123", Audience = IdentityApiFactory.SheepfoldAudience, Roles = ["Admin", "Pastor"] });

        var response = await SendToConsumerAsync(accessToken.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var whoAmI = await response.Content.ReadFromJsonAsync<WhoAmIResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(whoAmI);
        Assert.Equal("usuario-123", whoAmI.NameIdentifier);
        Assert.Equal(["Admin", "Pastor"], whoAmI.Roles.Order());
    }

    /// <summary>
    ///     Control de las pruebas de iss: el mismo token armado a mano, con el iss correcto, sí se acepta.
    /// </summary>
    [Fact]
    public async Task TokenWithConfiguredIssuer_SignedWithActiveKey_IsAccepted()
    {
        var token = TestTokens.Create(ActiveSigningCredentials, IdentityApiFactory.Issuer, IdentityApiFactory.SheepfoldAudience);

        var response = await SendToConsumerAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithPreviousPublishedKey_IsStillAccepted()
    {
        var credentials = TestKeys.Previous.CreateSigningCredentials(SheepfoldConsumerFixture.PreviousKeyId);
        var token = TestTokens.Create(credentials, IdentityApiFactory.Issuer, IdentityApiFactory.SheepfoldAudience);

        var response = await SendToConsumerAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithHs256_IsRejected()
    {
        var symmetricKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = ActiveSigningCredentials.Key.KeyId };
        var credentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);
        var token = TestTokens.Create(credentials, IdentityApiFactory.Issuer, IdentityApiFactory.SheepfoldAudience);

        var response = await SendToConsumerAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("invalid_token", GetAuthenticateError(response));
    }

    [Fact]
    public async Task TokenSignedWithUnpublishedRsaKey_IsRejected()
    {
        var credentials = TestRsaKey.Create(2048).CreateSigningCredentials("clave-no-publicada");
        var token = TestTokens.Create(credentials, IdentityApiFactory.Issuer, IdentityApiFactory.SheepfoldAudience);

        var response = await SendToConsumerAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(IdentityApiFactory.Issuer + "/")]
    [InlineData("https://IDENTITY.tmtbucaramanga.test")]
    [InlineData("http://identity.tmtbucaramanga.test")]
    [InlineData(IdentityApiFactory.Issuer + "/connect")]
    [InlineData("https://otro-identity.tmtbucaramanga.test")]
    public async Task TokenWhoseIssDiffersFromDiscoveryIssuer_IsRejected(string tokenIssuer)
    {
        var token = TestTokens.Create(ActiveSigningCredentials, tokenIssuer, IdentityApiFactory.SheepfoldAudience);

        var response = await SendToConsumerAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("issuer", GetAuthenticateError(response));
    }

    [Fact]
    public async Task TokenForAnotherAudience_IsRejected()
    {
        var issuer = IdentityServices.GetRequiredService<IAccessTokenIssuer>();
        var accessToken = issuer.Issue(new AccessTokenRequest { Subject = "usuario-123", Audience = IdentityApiFactory.OtherAudience, Roles = [] });

        var response = await SendToConsumerAsync(accessToken.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("audience", GetAuthenticateError(response));
    }

    /// <summary>
    ///     Emitido hace dos horas: con 60 minutos de vida venció hace una hora, más que el margen de reloj.
    /// </summary>
    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var issuer = TestTokens.CreateIssuerAt(IdentityServices, DateTimeOffset.UtcNow.AddHours(-2));
        var accessToken = issuer.Issue(new AccessTokenRequest { Subject = "usuario-123", Audience = IdentityApiFactory.SheepfoldAudience, Roles = [] });

        var response = await SendToConsumerAsync(accessToken.Token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("expired", GetAuthenticateError(response));
    }

    private SigningCredentials ActiveSigningCredentials =>
        IdentityServices.GetRequiredService<SigningKeyStore>().SigningCredentials;

    private async Task<HttpResponseMessage> SendToConsumerAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, SheepfoldConsumerFixture.WhoAmIPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _fixture.Consumer.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string GetAuthenticateError(HttpResponseMessage response)
    {
        var builder = new StringBuilder();

        foreach (var header in response.Headers.WwwAuthenticate)
        {
            builder.Append(header.Parameter);
        }

        return builder.ToString();
    }
}
