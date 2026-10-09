using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Queries;

/// <summary>
/// Directorio de cuentas vigentes para las APIs de la plataforma (Sheepfold). Solo lo consultan aplicaciones
/// con credenciales de servicio: Sheepfold decide qué datos ve cada usuario según su alcance ministerial y
/// usa el teléfono únicamente para las notificaciones por WhatsApp (§23).
/// </summary>
public class GetDirectoryUsersQuery : IRequest<List<DirectoryUserResponse>>
{
}

public sealed record DirectoryUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Gender? Gender,
    string? PhoneNumber,
    DateOnly? BirthDate,
    UserRole Role,
    bool IsActive,
    bool IsActivated,
    bool CanRegisterAttendees);

public class GetDirectoryUsersQueryHandler : IRequestHandler<GetDirectoryUsersQuery, List<DirectoryUserResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public GetDirectoryUsersQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<DirectoryUserResponse>> Handle(GetDirectoryUsersQuery request, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => new DirectoryUserResponse(
                user.Id, user.FirstName, user.LastName, user.Gender, user.PhoneNumber, user.BirthDate,
                user.Role, user.IsActive, user.ActivatedAt != null, user.CanRegisterAttendees))
            .ToListAsync(cancellationToken);
}
