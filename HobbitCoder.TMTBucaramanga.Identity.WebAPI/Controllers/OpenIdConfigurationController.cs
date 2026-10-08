using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// Documento de discovery de OpenID Connect: las APIs consumidoras lo descargan de
/// <c>{Authority}/.well-known/openid-configuration</c> para conocer el emisor esperado y el JWKS.
/// </summary>
/// <remarks>
/// <c>issuer</c> y <c>jwks_uri</c> salen de <see cref="JwtSettings.Issuer"/>, nunca del <c>Host</c> de la
/// petición, que puede cambiar detrás de un proxy. <c>issuer</c> es el mismo string del claim <c>iss</c>.
/// </remarks>
[Route(".well-known/openid-configuration")]
[ApiController]
[AllowAnonymous]
public class OpenIdConfigurationController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;

    public OpenIdConfigurationController(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    [HttpGet]
    [ProducesResponseType<OpenIdConfigurationResponse>(StatusCodes.Status200OK)]
    public ActionResult<OpenIdConfigurationResponse> GetOpenIdConfiguration() =>
        new OpenIdConfigurationResponse(
            Issuer: _jwtSettings.Issuer,
            JwksUri: _jwtSettings.Issuer + JsonWebKeySetController.Path,
            IdTokenSigningAlgValuesSupported: [SecurityAlgorithms.RsaSha256]);
}
