using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Auth;

public class AuthTests : IClassFixture<DatabaseApiFixture>
{
    private readonly DatabaseApiFixture _fixture;
    private readonly HttpClient _client;

    public AuthTests(DatabaseApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokensForRequestedAudienceAndProfile()
    {
        var email = AuthApi.NewEmail();
        var userId = await _client.RegisterAsync(email);

        var tokens = await _client.LoginAsync(email);

        var accessToken = new JsonWebToken(tokens.AccessToken);
        Assert.Equal(userId.ToString(), accessToken.Subject);
        Assert.Equal([IdentityApiFactory.IdentityAudience], accessToken.Audiences);
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.InRange(tokens.ExpiresIn, 3500, 3600);
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.ReadAsync<UserResponse>();
        Assert.Equal(userId, me.Id);
        Assert.Equal(email, me.Email);
        Assert.Empty(me.Roles);
    }

    [Fact]
    public async Task Register_IgnoresAdminOnlyFields()
    {
        var email = AuthApi.NewEmail();
        using var registered = await _client.PostRegisterAsync(new
        {
            email,
            password = AuthApi.Password,
            firstName = "Ana",
            lastName = "Pérez",
            roles = new[] { "Admin" },
            isActive = false,
            emailConfirmed = true
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var tokens = await _client.LoginAsync(email);
        using var response = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", tokens.AccessToken);
        var me = await response.ReadAsync<UserResponse>();

        Assert.Empty(me.Roles);
        Assert.True(me.IsActive);
        Assert.False(me.EmailConfirmed);
    }

    [Fact]
    public async Task Register_WithRegisteredEmailInOtherCase_Returns409()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        using var response = await _client.PostRegisterAsync(new { email = email.ToUpperInvariant(), password = AuthApi.Password, firstName = "Ana", lastName = "Pérez" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400OnPassword()
    {
        using var response = await _client.PostRegisterAsync(new { email = AuthApi.NewEmail(), password = "corta", firstName = "Ana", lastName = "Pérez" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401WithSameMessage()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        using var wrongPassword = await _client.PostLoginAsync(email, "Otra#Clave2026");
        using var unknownEmail = await _client.PostLoginAsync(AuthApi.NewEmail(), AuthApi.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal((await wrongPassword.ReadAsync<ProblemDetails>()).Detail, (await unknownEmail.ReadAsync<ProblemDetails>()).Detail);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksTheAccount()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await _client.PostLoginAsync(email, "Otra#Clave2026");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var response = await _client.PostLoginAsync(email, AuthApi.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForUnregisteredAudience_Returns400()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        using var response = await _client.PostLoginAsync(email, AuthApi.Password, "HobbitCoder.TMTBucaramanga.NoRegistrada");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_IsStoredOnlyAsSha256Hash()
    {
        var email = AuthApi.NewEmail();
        var userId = await _client.RegisterAsync(email);
        var tokens = await _client.LoginAsync(email);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var stored = await dbContext.RefreshTokens.SingleAsync(token => token.UserId == userId, CancellationToken);

        Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken))), stored.TokenHash);
        Assert.Equal(IdentityApiFactory.IdentityAudience, stored.Audience);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_AndReusingARotatedTokenRevokesTheSession()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var first = await _client.LoginAsync(email);

        var second = await _client.RefreshAsync(first.RefreshToken);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(new JsonWebToken(first.AccessToken).Id, new JsonWebToken(second.AccessToken).Id);

        using var reused = await _client.PostRefreshAsync(first.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);

        // La reutilización revocó toda la sesión, incluido el token legítimo más reciente.
        using var afterReuse = await _client.PostRefreshAsync(second.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var familyId = (await dbContext.RefreshTokens.FirstAsync(token => token.ReplacedByTokenId != null && token.User.Email == email, CancellationToken)).FamilyId;
        var reasons = await dbContext.RefreshTokens.Where(token => token.FamilyId == familyId).Select(token => token.RevocationReason).ToListAsync(CancellationToken);
        Assert.Equal([RefreshTokenRevocationReason.Rotated, RefreshTokenRevocationReason.ReuseDetected], reasons.Order());
    }

    [Fact]
    public async Task Refresh_ConcurrentRequestsWithSameToken_OnlyOneSucceeds()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var tokens = await _client.LoginAsync(email);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _client.PostRefreshAsync(tokens.RefreshToken)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.OK),
            response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task Refresh_ForAnotherRegisteredAudience_IssuesAccessTokenForThatApi()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var identityTokens = await _client.LoginAsync(email);

        var sheepfoldTokens = await _client.RefreshAsync(identityTokens.RefreshToken, IdentityApiFactory.SheepfoldAudience);

        Assert.Equal([IdentityApiFactory.SheepfoldAudience], new JsonWebToken(sheepfoldTokens.AccessToken).Audiences);

        // Un token para Sheepfold no sirve en los endpoints de Identity.
        using var me = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", sheepfoldTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        // La sesión conserva su audiencia: sin indicar otra, el siguiente refresh vuelve a ser para Identity.
        var next = await _client.RefreshAsync(sheepfoldTokens.RefreshToken);
        Assert.Equal([IdentityApiFactory.IdentityAudience], new JsonWebToken(next.AccessToken).Audiences);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        using var response = await _client.PostRefreshAsync("token-que-no-existe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheSession_AndIsIdempotent()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var tokens = await _client.LoginAsync(email);
        var otherSession = await _client.LoginAsync(email);

        using var logout = await _client.SendAsync(HttpMethod.Post, "/api/auth/logout", null, new { refreshToken = tokens.RefreshToken });
        using var repeated = await _client.SendAsync(HttpMethod.Post, "/api/auth/logout", null, new { refreshToken = tokens.RefreshToken });
        using var unknown = await _client.SendAsync(HttpMethod.Post, "/api/auth/logout", null, new { refreshToken = "token-que-no-existe" });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);

        using var refresh = await _client.PostRefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // Solo se cierra esa sesión: las demás del usuario siguen activas.
        await _client.RefreshAsync(otherSession.RefreshToken);
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPassword_AndClosesAllSessions()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var tokens = await _client.LoginAsync(email);
        const string newPassword = "Nueva#Clave2026";

        using var wrongCurrent = await _client.SendAsync(HttpMethod.Post, "/api/auth/change-password", tokens.AccessToken,
            new { currentPassword = "Otra#Clave2026", newPassword });
        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
        Assert.Contains("CurrentPassword", (await wrongCurrent.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        using var changed = await _client.SendAsync(HttpMethod.Post, "/api/auth/change-password", tokens.AccessToken,
            new { currentPassword = AuthApi.Password, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        using var refresh = await _client.PostRefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        using var oldPassword = await _client.PostLoginAsync(email, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        await _client.LoginAsync(email, newPassword);
    }

    [Fact]
    public async Task UpdateProfile_ChangesPersonalData()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var tokens = await _client.LoginAsync(email);

        using var update = await _client.SendAsync(HttpMethod.Put, "/api/auth/me", tokens.AccessToken,
            new { firstName = "María", lastName = "Gómez", phoneNumber = "+57 300 000 0000" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", tokens.AccessToken);
        var me = await response.ReadAsync<UserResponse>();
        Assert.Equal("María", me.FirstName);
        Assert.Equal("Gómez", me.LastName);
        Assert.Equal("+57 300 000 0000", me.PhoneNumber);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var response = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthResponse_ExpirationsMatchConfiguredLifetimes()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);

        var tokens = await _client.LoginAsync(email);

        Assert.Equal(new JsonWebToken(tokens.AccessToken).ValidTo, tokens.AccessTokenExpiresAt.UtcDateTime);
        Assert.InRange(tokens.RefreshTokenExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromDays(14) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(14));
    }
}
