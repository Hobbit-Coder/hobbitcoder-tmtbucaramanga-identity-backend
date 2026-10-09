using HobbitCoder.TMTBucaramanga.Identity.Application.Audit.Queries;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Controllers;

/// <summary>Auditoría de cuentas (§25). Solo directores; el administrador no consulta auditoría ministerial.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.Director)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("accounts")]
    [ProducesResponseType<PagedResult<AccountAuditEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AccountAuditEntryResponse>>> GetAccountAuditAsync([FromQuery] GetAccountAuditQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send<GetAccountAuditQuery, PagedResult<AccountAuditEntryResponse>>(query, cancellationToken));
    }
}
