using System.Text.Json.Serialization;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>Documento de <c>/.well-known/openid-configuration</c>.</summary>
public sealed record OpenIdConfigurationResponse(
    [property: JsonPropertyName("issuer")] string Issuer,
    [property: JsonPropertyName("jwks_uri")] string JwksUri,
    [property: JsonPropertyName("id_token_signing_alg_values_supported")] IReadOnlyList<string> IdTokenSigningAlgValuesSupported);

/// <summary>Documento de <c>/.well-known/jwks.json</c>.</summary>
public sealed record JsonWebKeySetResponse(
    [property: JsonPropertyName("keys")] IReadOnlyList<PublicJsonWebKey> Keys);

/// <summary>
/// Clave pública RSA en formato JWK. Solo tiene los parámetros públicos (<c>n</c>, <c>e</c>), así que
/// no puede exponer los privados aunque la clave de origen los tenga.
/// </summary>
public sealed record PublicJsonWebKey(
    [property: JsonPropertyName("kty")] string Kty,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("kid")] string Kid,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("e")] string E);
