using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Queries;

/// <summary>Cuentas vigentes paginadas, ordenadas por apellido y nombre.</summary>
public class GetUsersQuery : IRequest<PagedResult<UserResponse>>
{
    public const int MaxPageSize = 100;

    /// <summary>Texto que se busca en el nombre, el apellido, el usuario, el correo y el teléfono.</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public UserRole? Role { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetUsersQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<PagedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = GetUsersQueryHelpers.Search(_dbContext.Users, request.Search);

        if (request.IsActive is not null)
        {
            users = users.Where(user => user.IsActive == request.IsActive);
        }

        if (request.Role is not null)
        {
            users = users.Where(user => user.Role == request.Role);
        }

        return GetUsersQueryHelpers.PageAsync(_timeProvider, users, request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1");
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetUsersQuery.MaxPageSize).WithMessage($"PageSize must be between 1 and {GetUsersQuery.MaxPageSize}");
        RuleFor(query => query.Search)
            .MaximumLength(UserValidationRules.EmailMaxLength).WithMessage($"Search must not exceed {UserValidationRules.EmailMaxLength} characters");
        RuleFor(query => query.Role).IsInEnum().WithMessage("Role is not valid");
    }
}
