using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Elimina definitivamente una cuenta retirada o desactivada. La interfaz exige revisar y confirmar.
/// Se borran sus sesiones y solicitudes de verificación; la auditoría conserva el hecho. Los registros
/// ministeriales de Sheepfold que la referencian conservan su id (no hay claves foráneas entre bases).
/// </summary>
public class PermanentlyDeleteUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class PermanentlyDeleteUserCommandHandler : IRequestHandler<PermanentlyDeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public PermanentlyDeleteUserCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Atiende &lt;see cref="PermanentlyDeleteUserCommand"/&gt;.
    /// </summary>
    /// <remarks>
    ///     Con IsDeleted ya en true el interceptor no lo convierte en borrado lógico: el borrado es definitivo.
    /// </remarks>
    public async Task<Unit> Handle(PermanentlyDeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await UserLifecycleCommandHelpers.FindManageableAsync(_currentUserService, UserLifecycleCommandHelpers.RetiredUsers(_dbContext), request.Id, cancellationToken);
        _currentUserService.EnsureNotSelf(user, "delete");

        var before = AccountAudit.Snapshot(user);

        user.IsDeleted = true;
        _dbContext.Users.Remove(user);
        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.PermanentlyDeleted, user, before, after: null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class PermanentlyDeleteUserCommandValidator : AbstractValidator<PermanentlyDeleteUserCommand>
{
    public PermanentlyDeleteUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}
