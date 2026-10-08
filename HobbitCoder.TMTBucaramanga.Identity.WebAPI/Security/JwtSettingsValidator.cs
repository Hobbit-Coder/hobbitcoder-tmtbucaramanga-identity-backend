using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;

/// <summary>
/// Valida <see cref="JwtSettings"/> al arrancar (<c>ValidateOnStart</c>), incluida la carga de las
/// claves, para que una configuración inválida impida iniciar la aplicación en lugar de fallar al
/// emitir el primer token.
/// </summary>
public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    private const string Section = JwtSettings.SectionName;

    private readonly IHostEnvironment _environment;

    public JwtSettingsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        var failures = new List<string>();

        ValidateIssuer(options.Issuer, failures);
        ValidateAudiences(options.Audiences, failures);
        ValidateClients(options, failures);

        if (options.AccessTokenLifetimeMinutes <= 0)
        {
            failures.Add($"{Section}:AccessTokenLifetimeMinutes debe ser mayor que cero.");
        }

        if (options.RefreshTokenLifetimeDays <= 0)
        {
            failures.Add($"{Section}:RefreshTokenLifetimeDays debe ser mayor que cero.");
        }

        ValidateSigningKeys(options, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// El emisor se usa tal cual en <c>iss</c> y en el discovery, así que se exige en su forma final:
    /// URL absoluta, sin barra final, sin query ni fragmento y con <c>https</c> fuera de desarrollo.
    /// </summary>
    private void ValidateIssuer(string issuer, List<string> failures)
    {
        const string key = $"{Section}:Issuer";

        if (string.IsNullOrWhiteSpace(issuer))
        {
            failures.Add($"{key} es obligatorio: es la URL base pública de Identity.");
            return;
        }

        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add($"{key} '{issuer}' debe ser una URL absoluta http(s).");
            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && !_environment.IsDevelopment())
        {
            failures.Add($"{key} '{issuer}' debe usar https fuera de desarrollo.");
        }

        if (issuer != issuer.Trim() || issuer.EndsWith('/'))
        {
            failures.Add($"{key} '{issuer}' no debe terminar en '/' ni tener espacios: se usa tal cual en el claim iss.");
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            failures.Add($"{key} '{issuer}' no debe tener query ni fragmento.");
        }
    }

    private static void ValidateAudiences(List<string> audiences, List<string> failures)
    {
        if (audiences.Count == 0 || audiences.Exists(string.IsNullOrWhiteSpace))
        {
            failures.Add($"{Section}:Audiences debe tener al menos una API registrada y ningún valor vacío.");
        }
    }

    private void ValidateClients(JwtSettings options, List<string> failures)
    {
        foreach (var duplicated in options.Clients.GroupBy(client => client.ClientId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            failures.Add($"{Section}:Clients: el ClientId '{duplicated.Key}' está repetido.");
        }

        for (var index = 0; index < options.Clients.Count; index++)
        {
            var key = $"{Section}:Clients:{index}";
            var client = options.Clients[index];

            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                failures.Add($"{key}:ClientId es obligatorio.");
            }

            if (!options.Audiences.Contains(client.Audience, StringComparer.Ordinal))
            {
                failures.Add($"{key}:Audience '{client.Audience}' debe estar en {Section}:Audiences.");
            }

            if (client.RedirectUris.Count == 0)
            {
                failures.Add($"{key}:RedirectUris debe tener al menos una URI.");
            }

            foreach (var redirectUri in client.RedirectUris)
            {
                ValidateRedirectUri($"{key}:RedirectUris", redirectUri, failures);
            }
        }
    }

    /// <summary>
    /// Se exige en la forma exacta con la que la envía el cliente: se compara carácter por carácter y
    /// el código se agrega a su query.
    /// </summary>
    private void ValidateRedirectUri(string key, string redirectUri, List<string> failures)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add($"{key}: '{redirectUri}' debe ser una URL absoluta http(s).");
            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && !_environment.IsDevelopment())
        {
            failures.Add($"{key}: '{redirectUri}' debe usar https fuera de desarrollo.");
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            failures.Add($"{key}: '{redirectUri}' no debe tener query ni fragmento.");
        }
    }

    private void ValidateSigningKeys(JwtSettings options, List<string> failures)
    {
        var keyIds = new List<string>();

        TryLoadKey($"{Section}:SigningKey", () => RsaSigningKeyLoader.LoadPrivateKey(options.SigningKey, _environment.ContentRootPath), keyIds, failures);

        for (var index = 0; index < options.PreviousSigningKeys.Count; index++)
        {
            var settings = options.PreviousSigningKeys[index];
            TryLoadKey($"{Section}:PreviousSigningKeys:{index}", () => RsaSigningKeyLoader.LoadPublicKey(settings, _environment.ContentRootPath), keyIds, failures);
        }

        foreach (var duplicated in keyIds.GroupBy(keyId => keyId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            failures.Add($"El kid '{duplicated.Key}' está repetido: cada clave publicada necesita un kid distinto.");
        }
    }

    private static void TryLoadKey(string key, Func<RsaSecurityKey> load, List<string> keyIds, List<string> failures)
    {
        try
        {
            var securityKey = load();
            keyIds.Add(securityKey.KeyId);
            securityKey.Rsa.Dispose();
        }
        catch (InvalidOperationException ex)
        {
            failures.Add($"{key}: {ex.Message}");
        }
    }
}
