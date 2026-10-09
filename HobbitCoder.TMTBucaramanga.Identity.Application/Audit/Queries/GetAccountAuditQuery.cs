using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Audit.Queries;

/// <summary>
/// Auditoría de cuentas (§25): solo la consultan los directores, junto con la auditoría ministerial de
/// Sheepfold. Es de solo lectura.
/// </summary>
public class GetAccountAuditQuery : IRequest<PagedResult<AccountAuditEntryResponse>>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public AccountAuditAction? Action { get; set; }
    public string? ActorUserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AccountAuditEntryResponse
{
    public required Guid Id { get; set; }
    public required DateTime OccurredAt { get; set; }
    public required string ActorUserId { get; set; }
    public required AccountAuditAction Action { get; set; }
    public required Guid TargetUserId { get; set; }
    public required string TargetName { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}

public class GetAccountAuditQueryHandler : IRequestHandler<GetAccountAuditQuery, PagedResult<AccountAuditEntryResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetAccountAuditQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<AccountAuditEntryResponse>> Handle(GetAccountAuditQuery request, CancellationToken cancellationToken = default)
    {
        var entries = _dbContext.AccountAuditEntries.AsQueryable();

        if (request.From is not null)
        {
            entries = entries.Where(entry => entry.OccurredAt >= request.From);
        }

        if (request.To is not null)
        {
            entries = entries.Where(entry => entry.OccurredAt <= request.To);
        }

        if (request.Action is not null)
        {
            entries = entries.Where(entry => entry.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            entries = entries.Where(entry => entry.ActorUserId == request.ActorUserId);
        }

        return entries
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new AccountAuditEntryResponse
            {
                Id = entry.Id,
                OccurredAt = entry.OccurredAt,
                ActorUserId = entry.ActorUserId,
                Action = entry.Action,
                TargetUserId = entry.TargetUserId,
                TargetName = entry.TargetName,
                Before = entry.Before,
                After = entry.After
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetAccountAuditQueryValidator : AbstractValidator<GetAccountAuditQuery>
{
    public GetAccountAuditQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1");
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");
        RuleFor(query => query.Action).IsInEnum().WithMessage("Action is not valid");
    }
}
