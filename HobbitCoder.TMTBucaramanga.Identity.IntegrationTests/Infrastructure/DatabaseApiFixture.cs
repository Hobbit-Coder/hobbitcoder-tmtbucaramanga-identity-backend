using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Identity contra el PostgreSQL de <see cref="PostgreSqlFixture"/>, con el primer administrador creado
/// por el bootstrap y la pasarela de mensajería reemplazada por <see cref="CapturingSecurityMessageSender"/>.
/// </summary>
public sealed class DatabaseApiFixture : IAsyncLifetime
{
    public const string AdminUserName = "admin.tmt";
    public const string AdminPhoneNumber = "+573000000001";
    public const string AdminPassword = "Admin#Bootstrap2026";
    public const string ServiceClientId = "sheepfold-api";
    public const string ServiceClientSecret = "secreto-de-servicio-de-pruebas-con-32-caracteres";

    public DatabaseApiFixture(PostgreSqlFixture database)
    {
        Factory.Settings["Persistence:Provider"] = "PostgreSQL";
        Factory.Settings["ConnectionStrings:Default"] = database.ConnectionString;
        Factory.Settings["Bootstrap:Admin:UserName"] = AdminUserName;
        Factory.Settings["Bootstrap:Admin:PhoneNumber"] = AdminPhoneNumber;
        Factory.Settings["Bootstrap:Admin:Password"] = AdminPassword;
        Factory.Settings["Jwt:ServiceClients:0:ClientId"] = ServiceClientId;
        Factory.Settings["Jwt:ServiceClients:0:Secret"] = ServiceClientSecret;
        Factory.ConfigureServices = services => services.AddSingleton<ISecurityMessageSender>(Messages);
    }

    public IdentityApiFactory Factory { get; } = new();

    public CapturingSecurityMessageSender Messages { get; } = new();

    public HttpClient CreateClient() => Factory.CreateIssuerClient();

    public ValueTask InitializeAsync()
    {
        // Arranca el host: el bootstrap del administrador corre aquí.
        _ = Factory.Server;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => Factory.DisposeAsync();

    public static async Task<string> AdminTokenAsync(HttpClient client) =>
        (await client.LoginAsync(AdminUserName, AdminPassword)).AccessToken;

    /// <summary>
    /// Registra una cuenta como administrador y, si <paramref name="activate"/>, completa el primer ingreso
    /// con el código que "recibe" su titular.
    /// </summary>
    public async Task<TestAccount> CreateAccountAsync(HttpClient client, UserRole role = UserRole.Leader, string? email = null, bool activate = true)
    {
        var account = new TestAccount(AuthApi.NewUserName(), AuthApi.NewPhoneNumber(), email);

        using var created = await client.SendAsync(HttpMethod.Post, "/api/users", await AdminTokenAsync(client), new
        {
            firstName = "Ana",
            lastName = "Pérez",
            phoneNumber = account.PhoneNumber,
            userName = account.UserName,
            email,
            role,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        account.Id = await created.ReadAsync<Guid>();

        if (activate)
        {
            var verificationToken = await VerifyAsync(client, account, VerificationPurpose.Activation);
            using var activated = await client.SendAsync(HttpMethod.Post, "/api/account/activation", null,
                account.ActivationBody(verificationToken));
            Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);
        }

        return account;
    }

    /// <summary>Pide el código para la cuenta, lo lee de la pasarela de prueba y lo confirma.</summary>
    public async Task<string> VerifyAsync(HttpClient client, TestAccount account, VerificationPurpose purpose, VerificationChannel channel = VerificationChannel.Sms)
    {
        using var started = await client.SendAsync(HttpMethod.Post, "/api/account/verification", null,
            new { identifier = account.UserName, channel, purpose });
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var verificationId = (await started.ReadAsync<StartVerificationResponse>()).VerificationId;

        using var confirmed = await client.SendAsync(HttpMethod.Post, "/api/account/verification/confirm", null,
            new { verificationId, code = Messages.LastTo(account.PhoneNumber).Code });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);

        return (await confirmed.ReadAsync<ConfirmVerificationResponse>()).VerificationToken;
    }
}

/// <summary>Cuenta creada por una prueba, con los datos para iniciar sesión.</summary>
public sealed class TestAccount(string userName, string phoneNumber, string? email)
{
    public Guid Id { get; set; }
    public string UserName { get; } = userName;
    public string PhoneNumber { get; } = phoneNumber;
    public string? Email { get; } = email;
    public string Password { get; set; } = AuthApi.Password;

    /// <summary>Perfil completo para la activación (§5): todos los datos menos el correo, que es opcional.</summary>
    public object ActivationBody(string verificationToken, string? instagram = "ana.ejemplo") => new
    {
        verificationToken,
        password = Password,
        firstName = "Ana",
        lastName = "Pérez",
        gender = Gender.Female,
        phoneNumber = PhoneNumber,
        email = Email,
        userName = UserName,
        instagram,
        birthDate = new DateOnly(2000, 3, 15)
    };
}
