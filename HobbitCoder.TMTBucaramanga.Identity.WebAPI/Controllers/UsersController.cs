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
/// Administración completa de usuarios. Exige el rol <see cref="RoleNames.Admin"/> y un token emitido
/// para la audiencia de Identity.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.Admin)]
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
        var users = await _mediator.Send<GetUsersQuery, PagedResult<UserResponse>>(query, cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUserByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send<GetUserByIdQuery, UserResponse>(new GetUserByIdQuery { Id = id }, cancellationToken);

        return Ok(user);
    }

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

    /// <summary>Asigna una contraseña nueva, desbloquea la cuenta y cierra todas sus sesiones.</summary>
    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUserPasswordAsync([FromRoute] Guid id, [FromBody] SetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest();
        }

        await _mediator.Send<SetUserPasswordCommand, Unit>(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Borrado lógico; cierra todas las sesiones del usuario.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send<DeleteUserCommand, Unit>(new DeleteUserCommand { Id = id }, cancellationToken);

        return NoContent();
    }
}
