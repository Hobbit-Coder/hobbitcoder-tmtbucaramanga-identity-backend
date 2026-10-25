using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// Procesos de seguridad del titular: confirmar identidad con un código por SMS o WhatsApp, activar la
/// cuenta en el primer ingreso y recuperar el acceso creando una contraseña nueva. Son públicos: el titular
/// todavía no tiene sesión.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(Configuration.PublicSecurityRateLimit)]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Envía un código al teléfono de la cuenta. Responde igual exista o no la cuenta; 503 si el canal no está
    /// configurado.
    /// </summary>
    [HttpPost("verification")]
    [ProducesResponseType<StartVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<StartVerificationResponse>> StartVerificationAsync([FromBody] StartVerificationCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<StartVerificationCommand, StartVerificationResponse>(command, cancellationToken));
    }

    /// <summary>Confirma el código. Devuelve el token de verificación para crear la contraseña.</summary>
    [HttpPost("verification/confirm")]
    [ProducesResponseType<ConfirmVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfirmVerificationResponse>> ConfirmVerificationAsync([FromBody] ConfirmVerificationCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<ConfirmVerificationCommand, ConfirmVerificationResponse>(command, cancellationToken));
    }

    /// <summary>Datos ya registrados de la cuenta en activación (el perfil se completa con ellos).</summary>
    [HttpPost("activation/profile")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> GetActivationProfileAsync([FromBody] GetActivationProfileQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetActivationProfileQuery, UserResponse>(query, cancellationToken));
    }

    /// <summary>Crea la contraseña, completa el perfil y activa la cuenta.</summary>
    [HttpPost("activation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateAsync([FromBody] ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<ActivateAccountCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Crea una contraseña nueva después de confirmar la identidad y cierra las sesiones anteriores.</summary>
    [HttpPost("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send<ResetPasswordCommand, Unit>(command, cancellationToken);

        return NoContent();
    }
}
