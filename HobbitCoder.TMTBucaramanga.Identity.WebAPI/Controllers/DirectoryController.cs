using HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>
/// Directorio de cuentas para las APIs de la plataforma. Solo acepta tokens de servicio
/// (<c>POST /api/auth/service-token</c>); ninguna persona lo consulta directamente.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.Service)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class DirectoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DirectoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("users")]
    [ProducesResponseType<List<DirectoryUserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DirectoryUserResponse>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetDirectoryUsersQuery, List<DirectoryUserResponse>>(new GetDirectoryUsersQuery(), cancellationToken));
    }
}
