using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Account;

/// <summary>
/// Primer ingreso, recuperación y reinicio administrativo (§6, HU-01, HU-02, HU-03): la contraseña siempre la
/// crea el titular después de confirmar su identidad con un código enviado a su teléfono.
/// </summary>
public class AccountTests : IClassFixture<DatabaseApiFixture>
{
    private readonly DatabaseApiFixture _fixture;
    private readonly HttpClient _client;

    public AccountTests(DatabaseApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NewAccount_CannotSignInUntilItsHolderActivatesIt()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);

        using var beforeActivation = await _client.PostLoginAsync(account.UserName, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, beforeActivation.StatusCode);

        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Activation);
        using var profile = await _client.SendAsync(HttpMethod.Post, "/api/account/activation/profile", null, new { verificationToken });
        var current = await profile.ReadAsync<UserResponse>();
        Assert.Equal(account.UserName, current.UserName);
        Assert.Equal(AccessStatus.PendingActivation, current.AccessStatus);

        using var activated = await _client.SendAsync(HttpMethod.Post, "/api/account/activation", null, account.ActivationBody(verificationToken));
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);

        // Con teléfono o nombre de usuario (HU-02, casos 1 y 3).
        await _client.LoginAsync(account.PhoneNumber);
        await _client.LoginAsync(account.UserName);
        await _client.LoginAsync(account.PhoneNumber[3..]);
    }

    [Fact]
    public async Task Activation_WithoutEmail_DoesNotAllowSigningInWithAnEmail()
    {
        var account = await _fixture.CreateAccountAsync(_client);

        using var response = await _client.PostLoginAsync(AuthApi.NewEmail(), AuthApi.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _client.LoginAsync(account.UserName);
    }

    [Fact]
    public async Task Activation_WithEmail_AllowsSigningInWithTheEmail()
    {
        var email = AuthApi.NewEmail();
        await _fixture.CreateAccountAsync(_client, email: email);

        await _client.LoginAsync(email.ToUpperInvariant());
    }

    [Fact]
    public async Task InvalidCode_IsRejected_AndTheAccountKeepsItsState()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        using var started = await _client.SendAsync(HttpMethod.Post, "/api/account/verification", null,
            new { identifier = account.PhoneNumber, channel = VerificationChannel.WhatsApp, purpose = VerificationPurpose.Activation });
        var verificationId = (await started.ReadAsync<StartVerificationResponse>()).VerificationId;
        var sent = _fixture.Messages.LastTo(account.PhoneNumber);
        Assert.Equal(VerificationChannel.WhatsApp, sent.Channel);

        using var wrong = await _client.SendAsync(HttpMethod.Post, "/api/account/verification/confirm", null,
            new { verificationId, code = sent.Code == "000000" ? "111111" : "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Contains("Code", (await wrong.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        using var scope = _fixture.Factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<IdentityContext>().Users.SingleAsync(user => user.Id == account.Id, CancellationToken);
        Assert.Null(user.PasswordHash);
        Assert.Null(user.ActivatedAt);
    }

    [Fact]
    public async Task Code_StopsWorkingAfterTheMaximumAttempts()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        using var started = await _client.SendAsync(HttpMethod.Post, "/api/account/verification", null,
            new { identifier = account.UserName, channel = VerificationChannel.Sms, purpose = VerificationPurpose.Activation });
        var verificationId = (await started.ReadAsync<StartVerificationResponse>()).VerificationId;
        var code = _fixture.Messages.LastTo(account.PhoneNumber).Code;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var wrong = await _client.SendAsync(HttpMethod.Post, "/api/account/verification/confirm", null, new { verificationId, code = "x" + attempt });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        }

        using var right = await _client.SendAsync(HttpMethod.Post, "/api/account/verification/confirm", null, new { verificationId, code });
        Assert.Equal(HttpStatusCode.BadRequest, right.StatusCode);
    }

    [Fact]
    public async Task UnknownAccount_GetsTheSameResponse_ButNoMessageIsSent()
    {
        var phoneNumber = AuthApi.NewPhoneNumber();

        using var started = await _client.SendAsync(HttpMethod.Post, "/api/account/verification", null,
            new { identifier = phoneNumber, channel = VerificationChannel.Sms, purpose = VerificationPurpose.Activation });

        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.DoesNotContain(_fixture.Messages.Messages, message => message.PhoneNumber == phoneNumber);

        var verificationId = (await started.ReadAsync<StartVerificationResponse>()).VerificationId;
        using var confirm = await _client.SendAsync(HttpMethod.Post, "/api/account/verification/confirm", null, new { verificationId, code = "123456" });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    [Fact]
    public async Task Activation_RequiresTheCompleteProfile_AndLeavesTheAccountUnchangedWhenIncomplete()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Activation);

        using var incomplete = await _client.SendAsync(HttpMethod.Post, "/api/account/activation", null,
            account.ActivationBody(verificationToken, instagram: null));

        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        Assert.Contains("Instagram", (await incomplete.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        using var login = await _client.PostLoginAsync(account.UserName, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        // El mismo token sigue sirviendo para completar la activación.
        using var activated = await _client.SendAsync(HttpMethod.Post, "/api/account/activation", null, account.ActivationBody(verificationToken));
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);
    }

    [Fact]
    public async Task VerificationToken_IsSingleUse()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Activation);

        using var first = await _client.SendAsync(HttpMethod.Post, "/api/account/activation", null, account.ActivationBody(verificationToken));
        using var second = await _client.SendAsync(HttpMethod.Post, "/api/account/password", null, new { verificationToken, password = "Otra#Clave2026" });

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Recovery_LetsTheHolderCreateANewPassword_AndClosesOldSessions()
    {
        var account = await _fixture.CreateAccountAsync(_client);
        var session = await _client.LoginAsync(account.UserName);

        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Recovery);
        using var reset = await _client.SendAsync(HttpMethod.Post, "/api/account/password", null, new { verificationToken, password = "Nueva#Clave2026" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        using var oldSession = await _client.PostRefreshAsync(session.RefreshToken);
        using var oldPassword = await _client.PostLoginAsync(account.UserName, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        await _client.LoginAsync(account.UserName, "Nueva#Clave2026");
    }

    [Fact]
    public async Task AdministrativeReset_InvalidatesThePassword_UntilTheHolderCreatesANewOne()
    {
        var account = await _fixture.CreateAccountAsync(_client);
        var session = await _client.LoginAsync(account.UserName);

        using var reset = await _client.SendAsync(HttpMethod.Post, $"/api/users/{account.Id}/password-reset", await DatabaseApiFixture.AdminTokenAsync(_client));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        using var oldPassword = await _client.PostLoginAsync(account.UserName, AuthApi.Password);
        using var oldSession = await _client.PostRefreshAsync(session.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);

        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Recovery);
        using var created = await _client.SendAsync(HttpMethod.Post, "/api/account/password", null, new { verificationToken, password = "Propia#Clave2026" });
        Assert.Equal(HttpStatusCode.NoContent, created.StatusCode);
        await _client.LoginAsync(account.PhoneNumber, "Propia#Clave2026");
    }

    [Fact]
    public async Task Recovery_IsNotAvailableForAnAccountPendingActivation()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        var sentBefore = _fixture.Messages.Messages.Count(message => message.PhoneNumber == account.PhoneNumber);

        using var started = await _client.SendAsync(HttpMethod.Post, "/api/account/verification", null,
            new { identifier = account.UserName, channel = VerificationChannel.Sms, purpose = VerificationPurpose.Recovery });

        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(sentBefore, _fixture.Messages.Messages.Count(message => message.PhoneNumber == account.PhoneNumber));
    }

    [Fact]
    public async Task WeakPassword_IsRejectedWithoutActivatingTheAccount()
    {
        var account = await _fixture.CreateAccountAsync(_client, activate: false);
        var verificationToken = await _fixture.VerifyAsync(_client, account, VerificationPurpose.Activation);
        account.Password = "corta";

        using var response = await _client.SendAsync(HttpMethod.Post, "/api/account/activation", null, account.ActivationBody(verificationToken));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Password", (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }
}
