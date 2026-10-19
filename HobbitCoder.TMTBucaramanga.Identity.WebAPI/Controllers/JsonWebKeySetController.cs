using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// JWKS con las claves públicas de firma: la activa y las anteriores que siguen publicadas mientras
/// haya tokens vigentes firmados con ellas. Solo se exponen <c>n</c> y <c>e</c>, nunca los parámetros privados.
/// </summary>
[Route(Path)]
[ApiController]
[AllowAnonymous]
public class JsonWebKeySetController : ControllerBase
{
    public const string Path = "/.well-known/jwks.json";

    private readonly SigningKeyStore _signingKeyStore;

    public JsonWebKeySetController(SigningKeyStore signingKeyStore)
    {
        _signingKeyStore = signingKeyStore;
    }

    [HttpGet]
    [ProducesResponseType<JsonWebKeySetResponse>(StatusCodes.Status200OK)]
    public ActionResult<JsonWebKeySetResponse> GetJsonWebKeySet() =>
        new JsonWebKeySetResponse { Keys = [.. _signingKeyStore.PublishedKeys.Select(ToPublicJsonWebKey)] };

    private static PublicJsonWebKey ToPublicJsonWebKey(RsaSecurityKey key)
    {
        var parameters = key.Rsa.ExportParameters(includePrivateParameters: false);

        return new PublicJsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            Use = JsonWebKeyUseNames.Sig,
            Alg = SecurityAlgorithms.RsaSha256,
            Kid = key.KeyId,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };
    }
}
