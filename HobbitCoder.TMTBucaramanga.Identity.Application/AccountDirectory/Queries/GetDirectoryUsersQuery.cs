using HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.AccountDirectory.Queries;

/// <summary>
/// Directorio de cuentas vigentes para las APIs de la plataforma (Sheepfold). Solo lo consultan aplicaciones
/// con credenciales de servicio: Sheepfold decide qué datos ve cada usuario según su alcance ministerial y
/// usa el teléfono únicamente para las notificaciones por WhatsApp.
/// </summary>
public class GetDirectoryUsersQuery : IRequest<List<DirectoryUserResponse>>
{
}

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
            .Select(user => new DirectoryUserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Role = user.Role,
                IsActive = user.IsActive,
                IsActivated = user.ActivatedAt != null,
                CanRegisterAttendees = user.CanRegisterAttendees
            })
            .ToListAsync(cancellationToken);
}
