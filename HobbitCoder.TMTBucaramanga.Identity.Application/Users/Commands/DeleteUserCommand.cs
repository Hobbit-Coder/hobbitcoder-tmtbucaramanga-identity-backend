using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Elimina lógicamente un usuario y cierra todas sus sesiones. Su email sigue reservado: no se puede
/// registrar otra cuenta con él.
/// </summary>
public class DeleteUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteUserCommandHandler(IApplicationDbContext dbContext, AuthSessionService authSessionService, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        if (request.Id == _currentUserService.GetRequiredUserId())
        {
            throw new ConflictException("You cannot delete yourself.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        // El interceptor de auditoría convierte el Remove en borrado lógico.
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.UserDisabled, cancellationToken);

        return Unit.Value;
    }
}

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}
