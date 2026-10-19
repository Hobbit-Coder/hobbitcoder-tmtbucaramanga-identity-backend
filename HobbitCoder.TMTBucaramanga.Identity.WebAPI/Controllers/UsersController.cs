using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// Administración de cuentas. La usan el administrador y el director con un token para
/// la audiencia de Identity. Las cuentas con rol Administrador solo las administra otro administrador.
/// Ningún endpoint define contraseñas de terceros.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.AccountManagers)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetUsersAsync([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetUsersQuery, PagedResult<UserResponse>>(query, cancellationToken));
    }

    /// <summary>Cuentas retiradas o desactivadas.</summary>
    [HttpGet("retired")]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetRetiredUsersAsync([FromQuery] GetRetiredUsersQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetRetiredUsersQuery, PagedResult<UserResponse>>(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUserByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetUserByIdQuery, UserResponse>(new GetUserByIdQuery { Id = id }, cancellationToken));
    }

    /// <summary>Registra la cuenta sin contraseña: su titular la activa en el primer ingreso.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateUserAsync([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send<CreateUserCommand, Guid>(command, cancellationToken);

        return CreatedAtAction("GetUserById", new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUserAsync([FromRoute] Guid id, [FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await _mediator.Send<UpdateUserCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Solicita que el titular cree personalmente una contraseña nueva; cierra sus sesiones.</summary>
    [HttpPost("{id:guid}/password-reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestPasswordResetAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RequestPasswordResetCommand, Unit>(new RequestPasswordResetCommand { Id = id }, cancellationToken);

        return NoContent();
    }

    /// <summary>Retiro ordinario (borrado lógico); cierra sus sesiones.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<DeleteUserCommand, Unit>(new DeleteUserCommand { Id = id }, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreUserAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<RestoreUserCommand, Unit>(new RestoreUserCommand { Id = id }, cancellationToken);

        return NoContent();
    }

    /// <summary>Eliminación definitiva de una cuenta retirada o desactivada.</summary>
    [HttpDelete("{id:guid}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PermanentlyDeleteUserAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<PermanentlyDeleteUserCommand, Unit>(new PermanentlyDeleteUserCommand { Id = id }, cancellationToken);

        return NoContent();
    }
}
