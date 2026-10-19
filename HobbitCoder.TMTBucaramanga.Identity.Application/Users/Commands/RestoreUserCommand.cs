using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Restaura una cuenta retirada o desactivada: vuelve a las consultas habituales y queda
/// activa. Conserva su contraseña.
/// </summary>
public class RestoreUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public RestoreUserCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(RestoreUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await UserLifecycleCommandHelpers.FindManageableAsync(_currentUserService, UserLifecycleCommandHelpers.RetiredUsers(_dbContext), request.Id, cancellationToken);

        var before = AccountAudit.Snapshot(user);
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.DeletedBy = null;
        user.IsActive = true;

        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Restored, user, before, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}
