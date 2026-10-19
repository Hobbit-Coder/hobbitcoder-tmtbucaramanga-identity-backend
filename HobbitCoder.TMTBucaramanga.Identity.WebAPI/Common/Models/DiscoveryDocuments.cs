using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>Documento de <c>/.well-known/openid-configuration</c>.</summary>
public sealed class OpenIdConfigurationResponse
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; set; }
    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; set; }
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public required IReadOnlyList<string> IdTokenSigningAlgValuesSupported { get; set; }
}

/// <summary>Documento de <c>/.well-known/jwks.json</c>.</summary>
public sealed class JsonWebKeySetResponse
{
    [JsonPropertyName("keys")]
    public required IReadOnlyList<PublicJsonWebKey> Keys { get; set; }
}

/// <summary>
/// Clave pública RSA en formato JWK. Solo tiene los parámetros públicos (<c>n</c>, <c>e</c>), así que
/// no puede exponer los privados aunque la clave de origen los tenga.
/// </summary>
public sealed class PublicJsonWebKey
{
    [JsonPropertyName("kty")]
    public required string Kty { get; set; }
    [JsonPropertyName("use")]
    public required string Use { get; set; }
    [JsonPropertyName("alg")]
    public required string Alg { get; set; }
    [JsonPropertyName("kid")]
    public required string Kid { get; set; }
    [JsonPropertyName("n")]
    public required string N { get; set; }
    [JsonPropertyName("e")]
    public required string E { get; set; }
}
