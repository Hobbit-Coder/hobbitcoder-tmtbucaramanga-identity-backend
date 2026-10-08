using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Users;

public class UsersTests : IClassFixture<DatabaseApiFixture>
{
    private readonly HttpClient _client;

    public UsersTests(DatabaseApiFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task BootstrapAdmin_LogsInWithAdminRole()
    {
        var tokens = await _client.LoginAsync(DatabaseApiFixture.AdminEmail, DatabaseApiFixture.AdminPassword);

        var roles = new JsonWebToken(tokens.AccessToken).Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value);
        Assert.Contains(RoleNames.Admin, roles);
    }

    [Fact]
    public async Task Users_RequireAdminRole()
    {
        var email = AuthApi.NewEmail();
        await _client.RegisterAsync(email);
        var userTokens = await _client.LoginAsync(email);

        using var anonymous = await _client.SendAsync(HttpMethod.Get, "/api/users", accessToken: null);
        using var notAdmin = await _client.SendAsync(HttpMethod.Get, "/api/users", userTokens.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notAdmin.StatusCode);
    }

    [Fact]
    public async Task Users_RejectAdminTokenIssuedForAnotherApi()
    {
        var sheepfoldTokens = await _client.LoginAsync(DatabaseApiFixture.AdminEmail, DatabaseApiFixture.AdminPassword, IdentityApiFactory.SheepfoldAudience);

        using var response = await _client.SendAsync(HttpMethod.Get, "/api/users", sheepfoldTokens.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithAllFields_ThenGetById()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();

        using var created = await _client.SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            email,
            password = AuthApi.Password,
            firstName = "Carlos",
            lastName = "Rodríguez",
            phoneNumber = "3000000000",
            isActive = false,
            emailConfirmed = true,
            roles = new[] { "admin" }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.ReadAsync<Guid>();

        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users/{id}", adminToken);
        var user = await response.ReadAsync<UserResponse>();

        Assert.Equal(email, user.Email);
        Assert.Equal("Carlos", user.FirstName);
        Assert.False(user.IsActive);
        Assert.True(user.EmailConfirmed);
        Assert.Equal([RoleNames.Admin], user.Roles);
    }

    [Fact]
    public async Task CreateUser_WithUnknownRole_Returns400()
    {
        var adminToken = await GetAdminTokenAsync();

        using var response = await _client.SendAsync(HttpMethod.Post, "/api/users", adminToken, new
        {
            email = AuthApi.NewEmail(),
            password = AuthApi.Password,
            firstName = "Carlos",
            lastName = "Rodríguez",
            roles = new[] { "RolQueNoExiste" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Roles", (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task GetUsers_FiltersBySearchAndPaginates()
    {
        var adminToken = await GetAdminTokenAsync();
        var marker = Guid.NewGuid().ToString("N")[..12];
        var emails = new[] { $"{marker}-a@tmtbucaramanga.test", $"{marker}-b@tmtbucaramanga.test", $"{marker}-c@tmtbucaramanga.test" };

        foreach (var email in emails)
        {
            await _client.RegisterAsync(email);
        }

        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users?search={marker.ToUpperInvariant()}&pageSize=2&pageNumber=2", adminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadAsync<UserPage>();

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task GetUsers_FiltersByRole()
    {
        var adminToken = await GetAdminTokenAsync();

        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users?role={RoleNames.Admin}&pageSize=100", adminToken);
        var page = await response.ReadAsync<UserPage>();

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, user => Assert.Contains(RoleNames.Admin, user.Roles));
        Assert.Contains(page.Items, user => user.Email == DatabaseApiFixture.AdminEmail);
    }

    [Fact]
    public async Task UpdateUser_ChangesRoles_AndTheNextRefreshCarriesThem()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();
        var id = await _client.RegisterAsync(email);
        var userTokens = await _client.LoginAsync(email);

        using var update = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}", adminToken, new
        {
            id,
            email,
            firstName = "Laura",
            lastName = "Martínez",
            isActive = true,
            emailConfirmed = true,
            roles = new[] { RoleNames.Admin }
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var refreshed = await _client.RefreshAsync(userTokens.RefreshToken);
        Assert.Contains(new JsonWebToken(refreshed.AccessToken).Claims, claim => claim.Type == "role" && claim.Value == RoleNames.Admin);

        using var removeRoles = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}", adminToken, new
        {
            id,
            email,
            firstName = "Laura",
            lastName = "Martínez",
            isActive = true,
            emailConfirmed = true,
            roles = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.NoContent, removeRoles.StatusCode);

        using var response = await _client.SendAsync(HttpMethod.Get, $"/api/users/{id}", adminToken);
        var user = await response.ReadAsync<UserResponse>();
        Assert.Equal("Laura", user.FirstName);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public async Task UpdateUser_Deactivating_ClosesSessionsAndBlocksLogin()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();
        var id = await _client.RegisterAsync(email);
        var userTokens = await _client.LoginAsync(email);

        using var update = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}", adminToken, new
        {
            id, email, firstName = "Ana", lastName = "Pérez", isActive = false, emailConfirmed = false, roles = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        using var refresh = await _client.PostRefreshAsync(userTokens.RefreshToken);
        using var login = await _client.PostLoginAsync(email, AuthApi.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ToEmailOfAnotherUser_Returns409()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();
        var id = await _client.RegisterAsync(email);
        var otherEmail = AuthApi.NewEmail();
        await _client.RegisterAsync(otherEmail);

        using var response = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}", adminToken, new
        {
            id, email = otherEmail, firstName = "Ana", lastName = "Pérez", isActive = true, emailConfirmed = false, roles = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotDeactivateThemselvesOrRemoveTheirAdminRole()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminId = Guid.Parse(new JsonWebToken(adminToken).Subject);

        using var removeRole = await _client.SendAsync(HttpMethod.Put, $"/api/users/{adminId}", adminToken, new
        {
            id = adminId, email = DatabaseApiFixture.AdminEmail, firstName = "Admin", lastName = "TMT", isActive = true, emailConfirmed = true, roles = Array.Empty<string>()
        });
        using var delete = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{adminId}", adminToken);

        Assert.Equal(HttpStatusCode.Conflict, removeRole.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task SetUserPassword_ReplacesPassword_UnlocksAndClosesSessions()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();
        var id = await _client.RegisterAsync(email);
        var userTokens = await _client.LoginAsync(email);
        const string newPassword = "Asignada#Admin2026";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await _client.PostLoginAsync(email, "Otra#Clave2026");
        }

        using var weak = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}/password", adminToken, new { id, newPassword = "corta" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        using var set = await _client.SendAsync(HttpMethod.Put, $"/api/users/{id}/password", adminToken, new { id, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        using var refresh = await _client.PostRefreshAsync(userTokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        await _client.LoginAsync(email, newPassword);
    }

    [Fact]
    public async Task DeleteUser_SoftDeletes_BlocksLogin_AndKeepsEmailReserved()
    {
        var adminToken = await GetAdminTokenAsync();
        var email = AuthApi.NewEmail();
        var id = await _client.RegisterAsync(email);
        var userTokens = await _client.LoginAsync(email);

        using var delete = await _client.SendAsync(HttpMethod.Delete, $"/api/users/{id}", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var get = await _client.SendAsync(HttpMethod.Get, $"/api/users/{id}", adminToken);
        using var login = await _client.PostLoginAsync(email, AuthApi.Password);
        using var refresh = await _client.PostRefreshAsync(userTokens.RefreshToken);
        using var register = await _client.PostRegisterAsync(new { email, password = AuthApi.Password, firstName = "Ana", lastName = "Pérez" });

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, register.StatusCode);
    }

    private async Task<string> GetAdminTokenAsync() =>
        (await _client.LoginAsync(DatabaseApiFixture.AdminEmail, DatabaseApiFixture.AdminPassword)).AccessToken;

    /// <summary>JSON de <c>PagedResult&lt;UserResponse&gt;</c>, que no se puede deserializar directamente.</summary>
    private sealed record UserPage(List<UserResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);
}
