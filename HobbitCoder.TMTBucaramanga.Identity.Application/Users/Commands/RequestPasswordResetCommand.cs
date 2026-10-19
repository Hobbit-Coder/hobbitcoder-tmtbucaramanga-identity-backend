using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// La administración solicita que el titular vuelva a crear su contraseña. La contraseña actual
/// deja de servir y se cierran sus sesiones; el titular crea la nueva confirmando su identidad en
/// "Recuperar acceso". Nadie más la define ni recibe una contraseña temporal.
/// </summary>
public class RequestPasswordResetCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public RequestPasswordResetCommandHandler(
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

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken = default)
    {
        var user = await UserLifecycleCommandHelpers.FindManageableAsync(_currentUserService, _dbContext.Users, request.Id, cancellationToken);

        if (user.ActivatedAt is null)
        {
            throw new ConflictException("The account has not been activated yet: its holder creates the password on the first sign-in.");
        }

        var before = AccountAudit.Snapshot(user);
        user.ClearPassword();

        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.PasswordResetRequested, user, before, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.PasswordResetRequested, cancellationToken);

        return Unit.Value;
    }
}

public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}
