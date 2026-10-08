using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Queries;

/// <summary>Usuarios paginados, ordenados por apellido y nombre.</summary>
public class GetUsersQuery : IRequest<PagedResult<UserResponse>>
{
    public const int MaxPageSize = 100;

    /// <summary>Texto que se busca en el email, el nombre y el apellido.</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>Solo los usuarios con este rol.</summary>
    public string? Role { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetUsersQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PagedResult<UserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            users = users.Where(user => user.NormalizedEmail!.Contains(search)
                || user.FirstName.ToUpper().Contains(search)
                || user.LastName.ToUpper().Contains(search));
        }

        if (request.IsActive is not null)
        {
            users = users.Where(user => user.IsActive == request.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var normalizedRole = request.Role.Trim().ToUpperInvariant();
            users = users.Where(user => _dbContext.UserRoles
                .Where(userRole => userRole.UserId == user.Id)
                .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.NormalizedName)
                .Contains(normalizedRole));
        }

        return users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .SelectUserResponse(_dbContext)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, GetUsersQuery.MaxPageSize).WithMessage($"PageSize must be between 1 and {GetUsersQuery.MaxPageSize}");

        RuleFor(query => query.Search)
            .MaximumLength(UserValidationRules.EmailMaxLength).WithMessage($"Search must not exceed {UserValidationRules.EmailMaxLength} characters");
    }
}
