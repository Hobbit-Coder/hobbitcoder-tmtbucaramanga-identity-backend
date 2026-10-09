using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

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

/// <summary>Cuentas en registros retirados (§26): eliminadas lógicamente o desactivadas.</summary>
public class GetRetiredUsersQuery : IRequest<PagedResult<UserResponse>>
{
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetUsersQueryHandler :
    IRequestHandler<GetUsersQuery, PagedResult<UserResponse>>,
    IRequestHandler<GetRetiredUsersQuery, PagedResult<UserResponse>>
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
        var users = Search(_dbContext.Users, request.Search);

        if (request.IsActive is not null)
        {
            users = users.Where(user => user.IsActive == request.IsActive);
        }

        if (request.Role is not null)
        {
            users = users.Where(user => user.Role == request.Role);
        }

        return PageAsync(users, request.PageNumber, request.PageSize, cancellationToken);
    }

    public Task<PagedResult<UserResponse>> Handle(GetRetiredUsersQuery request, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users.IgnoreQueryFilters().Where(user => user.IsDeleted || !user.IsActive);

        return PageAsync(Search(users, request.Search), request.PageNumber, request.PageSize, cancellationToken);
    }

    private static IQueryable<User> Search(IQueryable<User> users, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return users;
        }

        var text = search.Trim().ToUpperInvariant();
        var phone = PhoneNumbers.Normalize(search);

        return users.Where(user => user.FirstName.ToUpper().Contains(text)
            || user.LastName.ToUpper().Contains(text)
            || user.NormalizedUserName!.Contains(text)
            || (user.NormalizedEmail != null && user.NormalizedEmail.Contains(text))
            || (phone != null && user.PhoneNumber == phone));
    }

    private async Task<PagedResult<UserResponse>> PageAsync(IQueryable<User> users, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(_timeProvider);

        var page = await users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .PaginatedListAsync(pageNumber, pageSize, cancellationToken);

        return page.Select(user => UserResponse.From(user, today));
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
