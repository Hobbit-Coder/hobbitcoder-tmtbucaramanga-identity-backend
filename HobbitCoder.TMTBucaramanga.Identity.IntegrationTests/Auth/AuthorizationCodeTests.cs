using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;
using System.Net.Http.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Auth;

/// <summary>
/// Inicio de sesión de una aplicación cliente (Sheepfold) con código de autorización y PKCE: el usuario
/// entra en Identity y el cliente recibe tokens de la misma sesión.
/// </summary>
public class AuthorizationCodeTests : IClassFixture<DatabaseApiFixture>
{
    private readonly HttpClient _client;

    public AuthorizationCodeTests(DatabaseApiFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Un usuario con sesión iniciada en Identity (audiencia de Identity).</summary>
    private async Task<AuthResponse> SignInToIdentityAsync()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        return await _client.LoginAsync(email);
    }

    [Fact]
    public async Task AuthorizeThenExchange_ReturnsClientTokensInTheSameIdentitySession()
    {
        var identity = await SignInToIdentityAsync();
        var pkce = PkceChallenge.Create();

        var code = await _client.AuthorizeSheepfoldAsync(identity.AccessToken, pkce);
        using var response = await _client.PostTokenAsync(code, pkce.CodeVerifier);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sheepfold = await response.ReadAsync<AuthResponse>();
        var identityToken = new JsonWebToken(identity.AccessToken);
        var sheepfoldToken = new JsonWebToken(sheepfold.AccessToken);
        Assert.Equal([IdentityApiFactory.SheepfoldAudience], sheepfoldToken.Audiences);
        Assert.Equal(identityToken.Subject, sheepfoldToken.Subject);
        Assert.Equal(identityToken.GetClaim(JwtRegisteredClaimNames.Sid).Value, sheepfoldToken.GetClaim(JwtRegisteredClaimNames.Sid).Value);

        // El refresh token del cliente renueva para su propia audiencia.
        var renewed = await _client.RefreshAsync(sheepfold.RefreshToken);
        Assert.Equal([IdentityApiFactory.SheepfoldAudience], new JsonWebToken(renewed.AccessToken).Audiences);
    }

    [Fact]
    public async Task LogoutFromClient_ClosesTheIdentitySession()
    {
        var identity = await SignInToIdentityAsync();
        var pkce = PkceChallenge.Create();
        var code = await _client.AuthorizeSheepfoldAsync(identity.AccessToken, pkce);
        using var exchange = await _client.PostTokenAsync(code, pkce.CodeVerifier);
        var sheepfold = await exchange.ReadAsync<AuthResponse>();

        using var logout = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = sheepfold.RefreshToken }, CancellationToken);
        using var identityRefresh = await _client.PostRefreshAsync(identity.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, identityRefresh.StatusCode);
    }

    [Fact]
    public async Task Exchange_WithReusedCode_Returns401AndRevokesTheSession()
    {
        var identity = await SignInToIdentityAsync();
        var pkce = PkceChallenge.Create();
        var code = await _client.AuthorizeSheepfoldAsync(identity.AccessToken, pkce);
        using var first = await _client.PostTokenAsync(code, pkce.CodeVerifier);

        using var second = await _client.PostTokenAsync(code, pkce.CodeVerifier);
        using var identityRefresh = await _client.PostRefreshAsync(identity.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, identityRefresh.StatusCode);
    }

    [Fact]
    public async Task Exchange_WithWrongVerifier_Returns401AndBurnsTheCode()
    {
        var identity = await SignInToIdentityAsync();
        var pkce = PkceChallenge.Create();
        var code = await _client.AuthorizeSheepfoldAsync(identity.AccessToken, pkce);

        using var wrongVerifier = await _client.PostTokenAsync(code, PkceChallenge.Create().CodeVerifier);
        using var rightVerifier = await _client.PostTokenAsync(code, pkce.CodeVerifier);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongVerifier.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, rightVerifier.StatusCode);
    }

    [Fact]
    public async Task Authorize_WithUnregisteredRedirectUri_Returns400()
    {
        var identity = await SignInToIdentityAsync();

        using var response = await _client.PostAuthorizeAsync(identity.AccessToken, PkceChallenge.Create(), "https://otro-sitio.test/auth/callback");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authorize_WithoutToken_Returns401()
    {
        using var response = await _client.PostAuthorizeAsync(identityAccessToken: null, PkceChallenge.Create());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authorize_AfterLogout_Returns401EvenWithAValidAccessToken()
    {
        var identity = await SignInToIdentityAsync();
        using var logout = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = identity.RefreshToken }, CancellationToken);

        using var response = await _client.PostAuthorizeAsync(identity.AccessToken, PkceChallenge.Create());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
