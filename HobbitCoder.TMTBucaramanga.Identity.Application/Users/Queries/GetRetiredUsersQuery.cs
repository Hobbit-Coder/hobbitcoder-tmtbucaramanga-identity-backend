using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Queries;

/// <summary>Cuentas en registros retirados: eliminadas lógicamente o desactivadas.</summary>
public class GetRetiredUsersQuery : IRequest<PagedResult<UserResponse>>
{
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetRetiredUsersQueryHandler : IRequestHandler<GetRetiredUsersQuery, PagedResult<UserResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetRetiredUsersQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<PagedResult<UserResponse>> Handle(GetRetiredUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users.IgnoreQueryFilters().Where(user => user.IsDeleted || !user.IsActive);

        return GetUsersQueryHelpers.PageAsync(_timeProvider, GetUsersQueryHelpers.Search(users, request.Search), request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetRetiredUsersQueryValidator : AbstractValidator<GetRetiredUsersQuery>
{
    public GetRetiredUsersQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1");
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetUsersQuery.MaxPageSize).WithMessage($"PageSize must be between 1 and {GetUsersQuery.MaxPageSize}");
        RuleFor(query => query.Search)
            .MaximumLength(UserValidationRules.EmailMaxLength).WithMessage($"Search must not exceed {UserValidationRules.EmailMaxLength} characters");
    }
}
