using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// Registro, inicio y cierre de sesión, renovación de tokens y la cuenta del usuario autenticado.
/// Los endpoints de la cuenta (<c>me</c>, <c>change-password</c>) exigen un token emitido para la
/// audiencia de Identity.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registro público limitado: usuario activo, sin roles y con el email sin confirmar.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> RegisterAsync([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<RegisterCommand, Guid>(command, cancellationToken);

        return CreatedAtAction("GetCurrentUser", null, id);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send<LoginCommand, AuthResponse>(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>Cambia un refresh token por un par nuevo; el usado queda revocado.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshAsync([FromBody] RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send<RefreshSessionCommand, AuthResponse>(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Autoriza a una aplicación cliente a entrar con la sesión actual de Identity (código de
    /// autorización con PKCE). Lo llama el frontend de Identity; el navegador vuelve luego al cliente.
    /// </summary>
    [HttpPost("authorize")]
    [Authorize]
    [ProducesResponseType<AuthorizeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthorizeResponse>> AuthorizeAsync([FromBody] AuthorizeCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send<AuthorizeCommand, AuthorizeResponse>(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>Canjea un código de autorización por el par de tokens del cliente. El código no se puede reutilizar.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> ExchangeAuthorizationCodeAsync([FromBody] ExchangeAuthorizationCodeCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send<ExchangeAuthorizationCodeCommand, AuthResponse>(command, cancellationToken);

        return Ok(response);
    }

    /// <summary>Cierra la sesión del refresh token. Responde 204 aunque el token no exista.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogoutAsync([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<LogoutCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send<GetCurrentUserQuery, UserResponse>(new GetCurrentUserQuery(), cancellationToken);

        return Ok(user);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<UpdateProfileCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Cambia la contraseña y cierra todas las sesiones del usuario.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<ChangePasswordCommand, Unit>(command, cancellationToken);

        return NoContent();
    }
}
