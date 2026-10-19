using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Retira una cuenta (eliminación ordinaria): sale de las consultas habituales, se conserva en
/// registros retirados y se cierran sus sesiones. Sus identificadores siguen reservados.
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
    private readonly TimeProvider _timeProvider;

    public DeleteUserCommandHandler(
        IApplicationDbContext dbContext,
        AuthSessionService authSessionService,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Atiende &lt;see cref="DeleteUserCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     El interceptor de auditoría convierte el Remove en borrado lógico.
    /// </remarks>
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await UserLifecycleCommandHelpers.FindManageableAsync(_currentUserService, _dbContext.Users, request.Id, cancellationToken);
        _currentUserService.EnsureNotSelf(user, "retire");

        var before = AccountAudit.Snapshot(user);

        _dbContext.Users.Remove(user);
        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Retired, user, before, after: null);
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
