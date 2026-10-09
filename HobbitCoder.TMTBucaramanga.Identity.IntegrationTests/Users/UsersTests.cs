using HobbitCoder.TMTBucaramanga.Identity.Application.Audit.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;
using System.Net.Http.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Users;

/// <summary>Administración de cuentas (HU-03), directorio para Sheepfold y auditoría de cuentas (HU-27).</summary>
public class UsersTests : IClassFixture<DatabaseApiFixture>
{
    private readonly DatabaseApiFixture _fixture;
    private readonly HttpClient _client;

    public UsersTests(DatabaseApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<string> AdminTokenAsync() => DatabaseApiFixture.AdminTokenAsync(_client);

    private async Task<string> TokenForAsync(UserRole role)
    {
        var account = await _fixture.CreateAccountAsync(_client, role);
        return (await _client.LoginAsync(account.UserName)).AccessToken;
    }

    private static object NewUserBody(UserRole role = UserRole.Leader, string? userName = null, string? phoneNumber = null, string? email = null) => new
    {
        firstName = "Daniel",
        lastName = "Torres",
        gender = Gender.Male,
        phoneNumber = phoneNumber ?? AuthApi.NewPhoneNumber(),
        email,
        userName = userName ?? AuthApi.NewUserName(),
        instagram = "daniel.ejemplo",
        birthDate = new DateOnly(1998, 3, 15),
        role,
        isActive = true,
        canRegisterAttendees = true
    };

    [Fact]
    public async Task BootstrapAdmin_LogsInWithAdminRole()
    {
        var tokens = await _client.LoginAsync(DatabaseApiFixture.AdminUserName, DatabaseApiFixture.AdminPassword);

        var roles = new JsonWebToken(tokens.AccessToken).Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value);
        Assert.Equal([RoleNames.Admin], roles);
    }

    [Fact]
    public async Task Users_RequireAdminOrDirector()
    {
        using var anonymous = await _client.SendAsync(HttpMethod.Get, "/api/users", accessToken: null);
        using var leader = await _client.SendAsync(HttpMethod.Get, "/api/users", await TokenForAsync(UserRole.Leader));
        using var coordinator = await _client.SendAsync(HttpMethod.Get, "/api/users", await TokenForAsync(UserRole.Coordinator));
        using var director = await _client.SendAsync(HttpMethod.Get, "/api/users", await TokenForAsync(UserRole.Director));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, leader.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, coordinator.StatusCode);
        Assert.Equal(HttpStatusCode.OK, director.StatusCode);
    }

    [Fact]
    public async Task Users_RejectAdminTokenIssuedForAnotherApi()
    {
        var sheepfoldTokens = await _client.LoginAsync(DatabaseApiFixture.AdminUserName, DatabaseApiFixture.AdminPassword, IdentityApiFactory.SheepfoldAudience);

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/users", sheepfoldTokens.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_RegistersTheAccountWithoutPassword_ThenGetById()
    {
        var userName = AuthApi.NewUserName();
        using var created = await _client.SendAsync(HttpMethod.Post, "/api/users", await TokenForAsync(UserRole.Director),
            NewUserBody(userName: userName));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.ReadAsync<Guid>();

        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users/{id}", await AdminTokenAsync());
        var user = await response.ReadAsync<UserResponse>();

        Assert.Equal(userName, user.UserName);
        Assert.Equal(UserRole.Leader, user.Role);
        Assert.Equal(Gender.Male, user.Gender);
        Assert.True(user.CanRegisterAttendees);
        Assert.Equal(AccessStatus.PendingActivation, user.AccessStatus);
        Assert.NotNull(user.Age);

        // Sin contraseña nadie puede entrar con esa cuenta hasta que su titular la active.
        using var login = await _client.PostLoginAsync(userName, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task CreateUser_NormalizesThePhoneNumber()
    {
        var phoneNumber = AuthApi.NewPhoneNumber();
        var national = $"{phoneNumber[3..6]} {phoneNumber[6..9]} {phoneNumber[9..]}";

        using var created = await _client.SendAsync(HttpMethod.Post, "/api/users", await AdminTokenAsync(), NewUserBody(phoneNumber: national));
        var id = await created.ReadAsync<Guid>();
        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users/{id}", await AdminTokenAsync());

        Assert.Equal(phoneNumber, (await response.ReadAsync<UserResponse>()).PhoneNumber);
    }

    [Fact]
    public async Task CreateUser_WithRepeatedIdentifiers_Returns409()
    {
        var token = await AdminTokenAsync();
        var userName = AuthApi.NewUserName();
        var phoneNumber = AuthApi.NewPhoneNumber();
        var email = AuthApi.NewEmail();
        using var first = await _client.SendAsync(HttpMethod.Post, "/api/users", token, NewUserBody(userName: userName, phoneNumber: phoneNumber, email: email));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var sameUserName = await _client.SendAsync(HttpMethod.Post, "/api/users", token, NewUserBody(userName: userName.ToUpperInvariant()));
        using var samePhone = await _client.SendAsync(HttpMethod.Post, "/api/users", token, NewUserBody(phoneNumber: phoneNumber));
        using var sameEmail = await _client.SendAsync(HttpMethod.Post, "/api/users", token, NewUserBody(email: email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, sameUserName.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, samePhone.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, sameEmail.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidData_Returns400()
    {
        using var response = await _client.SendAsync(HttpMethod.Post, "/api/users", await AdminTokenAsync(), new
        {
            firstName = "",
            lastName = "Torres",
            phoneNumber = "abc",
            userName = "12345",
            role = UserRole.Leader
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys;
        Assert.Contains("FirstName", errors);
        Assert.Contains("PhoneNumber", errors);
        Assert.Contains("UserName", errors);
    }

    [Fact]
    public async Task Director_CannotManageAdministratorAccounts()
    {
        var directorToken = await TokenForAsync(UserRole.Director);

        using var response = await _client.SendAsync(HttpMethod.Post, "/api/users", directorToken, NewUserBody(UserRole.Admin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ChangesTheRole_AndTheNextRefreshCarriesIt()
    {
        var account = await _fixture.CreateAccountAsync(_client, UserRole.Leader);
        var tokens = await _client.LoginAsync(account.UserName);

        using var update = await _client.SendAsync(HttpMethod.Put, $"/api/users/{account.Id}", await AdminTokenAsync(), new
        {
            id = account.Id,
            firstName = "Ana",
            lastName = "Pérez",
            gender = Gender.Female,
            phoneNumber = account.PhoneNumber,
            userName = account.UserName,
            instagram = "ana.ejemplo",
            birthDate = new DateOnly(2000, 3, 15),
            role = UserRole.Coordinator,
            isActive = true,
            canRegisterAttendees = true
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var refreshed = new JsonWebToken((await _client.RefreshAsync(tokens.RefreshToken)).AccessToken);
        Assert.Equal([RoleNames.Coordinator], refreshed.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value));
        Assert.Contains(refreshed.Claims, claim => claim.Type == PermissionNames.ClaimType && claim.Value == PermissionNames.RegisterAttendees);
    }

    [Fact]
    public async Task Deactivation_RejectsSignIn_AndClosesSessions_ButKeepsTheAccount()
    {
        var account = await _fixture.CreateAccountAsync(_client);
        var tokens = await _client.LoginAsync(account.UserName);
        var adminToken = await AdminTokenAsync();

        using var current = await _client.SendAsync(HttpMethod.Get, $"/api/users/{account.Id}", adminToken);
        var user = await current.ReadAsync<UserResponse>();
        using var deactivate = await _client.SendAsync(HttpMethod.Put, $"/api/users/{account.Id}", adminToken, new
        {
            id = user.Id,
            user.FirstName,
            user.LastName,
            user.Gender,
            user.PhoneNumber,
            user.UserName,
            user.Instagram,
            user.BirthDate,
            user.Role,
            isActive = false
        });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        using var login = await _client.PostLoginAsync(account.UserName, AuthApi.Password);
        using var refresh = await _client.PostRefreshAsync(tokens.RefreshToken);
        using var wrongPassword = await _client.PostLoginAsync(account.UserName, "Otra#Clave123");
        // Con la contraseña correcta se informa que el acceso no está disponible (HU-02); si no, respuesta genérica.
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        using var retired = await _client.SendAsync(HttpMethod.Get, $"/api/users/retired?search={account.UserName}", adminToken);
        Assert.Contains((await retired.ReadAsync<Page<UserResponse>>()).Items, item => item.Id == account.Id);
    }

    [Fact]
    public async Task Admin_CannotRetireOrDeactivateThemselves()
    {
        var adminToken = await AdminTokenAsync();
        var adminId = new JsonWebToken(adminToken).Subject;

        using var retire = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{adminId}", adminToken);

        Assert.Equal(HttpStatusCode.Conflict, retire.StatusCode);
    }

    [Fact]
    public async Task RetireRestoreAndPermanentDelete_FollowTheConservationRules()
    {
        var account = await _fixture.CreateAccountAsync(_client);
        var adminToken = await AdminTokenAsync();

        using var notRetired = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{account.Id}/permanent", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, notRetired.StatusCode);

        using var retire = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{account.Id}", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, retire.StatusCode);
        using var hidden = await _client.SendAsync(HttpMethod.Get, $"/api/users/{account.Id}", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        using var restore = await _client.SendAsync(HttpMethod.Post, $"/api/users/{account.Id}/restore", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);
        await _client.LoginAsync(account.UserName);

        using var retireAgain = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{account.Id}", adminToken);
        using var permanent = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{account.Id}/permanent", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, permanent.StatusCode);

        using var gone = await _client.SendAsync(HttpMethod.Post, $"/api/users/{account.Id}/restore", adminToken);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task GetUsers_FiltersByRole()
    {
        await _fixture.CreateAccountAsync(_client, UserRole.Apprentice);

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/users?role=Apprentice&pageSize=100", await AdminTokenAsync());

        var page = await response.ReadAsync<Page<UserResponse>>();
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, user => Assert.Equal(UserRole.Apprentice, user.Role));
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheOwnProfile_ButNotTheRole()
    {
        var account = await _fixture.CreateAccountAsync(_client);
        var tokens = await _client.LoginAsync(account.UserName);

        using var update = await _client.SendAsync(HttpMethod.Put, "/api/auth/me", tokens.AccessToken, new
        {
            firstName = "María",
            lastName = "Gómez",
            gender = Gender.Female,
            phoneNumber = account.PhoneNumber,
            email = (string?)null,
            userName = account.UserName,
            instagram = "maria.ejemplo",
            birthDate = new DateOnly(1999, 1, 2),
            role = UserRole.Director
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/auth/me", tokens.AccessToken);
        var me = await response.ReadAsync<UserResponse>();
        Assert.Equal("María", me.FirstName);
        Assert.Equal("maria.ejemplo", me.Instagram);
        Assert.Equal(UserRole.Leader, me.Role);
    }

    [Fact]
    public async Task Directory_IsOnlyAvailableWithServiceCredentials()
    {
        await _fixture.CreateAccountAsync(_client, UserRole.Coordinator);

        using var wrongSecret = await _client.PostAsJsonAsync("/api/auth/service-token",
            new { clientId = DatabaseApiFixture.ServiceClientId, clientSecret = "otro-secreto" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);

        using var asAdmin = await _client.SendAsync(HttpMethod.Get, "/api/directory/users", await AdminTokenAsync());
        Assert.Equal(HttpStatusCode.Forbidden, asAdmin.StatusCode);

        using var serviceToken = await _client.PostAsJsonAsync("/api/auth/service-token",
            new { clientId = DatabaseApiFixture.ServiceClientId, clientSecret = DatabaseApiFixture.ServiceClientSecret }, CancellationToken);
        var token = (await serviceToken.ReadAsync<ServiceTokenBody>()).AccessToken;

        using var directory = await _client.SendAsync(HttpMethod.Get, "/api/directory/users", token);
        Assert.Equal(HttpStatusCode.OK, directory.StatusCode);
        Assert.Contains((await directory.ReadAsync<List<DirectoryUserResponse>>()), user => user.Role == UserRole.Coordinator && user.IsActivated);
    }

    [Fact]
    public async Task AccountAudit_RecordsChanges_AndIsOnlyForDirectors()
    {
        var account = await _fixture.CreateAccountAsync(_client);

        using var asAdmin = await _client.SendAsync(HttpMethod.Get, "/api/audit/accounts", await AdminTokenAsync());
        Assert.Equal(HttpStatusCode.Forbidden, asAdmin.StatusCode);

        using var asDirector = await _client.SendAsync(HttpMethod.Get, "/api/audit/accounts?pageSize=100", await TokenForAsync(UserRole.Director));
        var entries = (await asDirector.ReadAsync<Page<AccountAuditEntryResponse>>()).Items.Where(entry => entry.TargetUserId == account.Id).ToList();

        Assert.Contains(entries, entry => entry.Action == AccountAuditAction.Created);
        Assert.Contains(entries, entry => entry.Action == AccountAuditAction.AccountActivated);
        Assert.All(entries, entry => Assert.DoesNotContain("password", (entry.After ?? string.Empty).ToLowerInvariant()));
    }

    private sealed record ServiceTokenBody(string AccessToken);
}
