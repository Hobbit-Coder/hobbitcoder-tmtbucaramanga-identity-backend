using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>Atajos para llamar a <c>/api/auth</c> y a endpoints protegidos.</summary>
public static class AuthApi
{
    public const string Password = "Clave#Segura2026";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Opciones JSON de la API: enums como texto.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static string NewEmail() => $"usuario-{Guid.NewGuid():N}@tmtbucaramanga.test";

    public static string NewUserName() => $"usuario.{Guid.NewGuid():N}"[..30];

    /// <summary>Celular colombiano único (+57 3xx xxx xxxx) para las pruebas.</summary>
    public static string NewPhoneNumber() => $"+573{Random.Shared.NextInt64(100_000_000, 999_999_999)}";

    public static Task<HttpResponseMessage> PostLoginAsync(this HttpClient client, string identifier, string password, string audience = IdentityApiFactory.IdentityAudience) =>
        client.PostAsJsonAsync("/api/auth/login", new { identifier, password, audience }, CancellationToken);

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string identifier, string password = Password, string audience = IdentityApiFactory.IdentityAudience)
    {
        using var response = await client.PostLoginAsync(identifier, password, audience);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<AuthResponse>(CancellationToken))!;
    }

    public static Task<HttpResponseMessage> PostRefreshAsync(this HttpClient client, string refreshToken, string? audience = null) =>
        client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken, audience }, CancellationToken);

    public static async Task<AuthResponse> RefreshAsync(this HttpClient client, string refreshToken, string? audience = null)
    {
        using var response = await client.PostRefreshAsync(refreshToken, audience);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<AuthResponse>(CancellationToken))!;
    }

    /// <summary>
    /// Autoriza al cliente de Sheepfold con la sesión del token de Identity y devuelve el código de la
    /// URI de retorno, comprobando que trae el mismo <c>state</c>.
    /// </summary>
    public static async Task<string> AuthorizeSheepfoldAsync(this HttpClient client, string identityAccessToken, PkceChallenge pkce)
    {
        const string state = "estado-del-cliente";

        using var response = await client.PostAuthorizeAsync(identityAccessToken, pkce);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var redirectUri = new Uri((await response.ReadAsync<AuthorizeResponse>()).RedirectUri);
        var query = QueryHelpers.ParseQuery(redirectUri.Query);
        Assert.Equal(IdentityApiFactory.SheepfoldRedirectUri, redirectUri.GetLeftPart(UriPartial.Path));
        Assert.Equal(state, query["state"].ToString());

        return query["code"].ToString();
    }

    public static Task<HttpResponseMessage> PostAuthorizeAsync(
        this HttpClient client, string? identityAccessToken, PkceChallenge pkce, string redirectUri = IdentityApiFactory.SheepfoldRedirectUri) =>
        client.SendAsync(HttpMethod.Post, "/api/auth/authorize", identityAccessToken, new
        {
            clientId = IdentityApiFactory.SheepfoldClientId,
            redirectUri,
            codeChallenge = pkce.CodeChallenge,
            codeChallengeMethod = "S256",
            state = "estado-del-cliente"
        });

    public static Task<HttpResponseMessage> PostTokenAsync(this HttpClient client, string code, string codeVerifier) =>
        client.PostAsJsonAsync("/api/auth/token", new
        {
            code,
            codeVerifier,
            clientId = IdentityApiFactory.SheepfoldClientId,
            redirectUri = IdentityApiFactory.SheepfoldRedirectUri
        }, CancellationToken);

    /// <summary>Envía una petición con <c>Authorization: Bearer</c>.</summary>
    public static async Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string uri, string? accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri);

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, CancellationToken);
        Assert.NotNull(content);

        return content;
    }
}

/// <summary>Página de resultados tal como la serializa la API (<c>PagedResult</c>).</summary>
public sealed class Page<T>
{
    public required List<T> Items { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public required int TotalPages { get; set; }
}
