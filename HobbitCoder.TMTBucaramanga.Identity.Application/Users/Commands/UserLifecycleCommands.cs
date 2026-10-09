using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// La administración solicita que el titular vuelva a crear su contraseña (§6, §4.5). La contraseña actual
/// deja de servir y se cierran sus sesiones; el titular crea la nueva confirmando su identidad en
/// "Recuperar acceso". Nadie más la define ni recibe una contraseña temporal.
/// </summary>
public class RequestPasswordResetCommand : IRequest
{
    public required Guid Id { get; set; }
}

/// <summary>
/// Retira una cuenta (eliminación ordinaria, §26): sale de las consultas habituales, se conserva en
/// registros retirados y se cierran sus sesiones. Sus identificadores siguen reservados.
/// </summary>
public class DeleteUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

/// <summary>
/// Restaura una cuenta retirada o desactivada (§26, decisión P05): vuelve a las consultas habituales y queda
/// activa. Conserva su contraseña.
/// </summary>
public class RestoreUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

/// <summary>
/// Elimina definitivamente una cuenta retirada o desactivada (§26). La interfaz exige revisar y confirmar.
/// Se borran sus sesiones y solicitudes de verificación; la auditoría conserva el hecho. Los registros
/// ministeriales de Sheepfold que la referencian conservan su id (no hay claves foráneas entre bases).
/// </summary>
public class PermanentlyDeleteUserCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class UserLifecycleCommandHandlers :
    IRequestHandler<RequestPasswordResetCommand, Unit>,
    IRequestHandler<DeleteUserCommand, Unit>,
    IRequestHandler<RestoreUserCommand, Unit>,
    IRequestHandler<PermanentlyDeleteUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UserLifecycleCommandHandlers(
        IApplicationDbContext dbContext, AuthSessionService authSessionService, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken = default)
    {
        var user = await FindManageableAsync(_dbContext.Users, request.Id, cancellationToken);

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

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await FindManageableAsync(_dbContext.Users, request.Id, cancellationToken);
        _currentUserService.EnsureNotSelf(user, "retire");

        var before = AccountAudit.Snapshot(user);

        // El interceptor de auditoría convierte el Remove en borrado lógico.
        _dbContext.Users.Remove(user);
        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Retired, user, before, after: null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.UserDisabled, cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(RestoreUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await FindManageableAsync(RetiredUsers(), request.Id, cancellationToken);

        var before = AccountAudit.Snapshot(user);
        user.IsDeleted = false;
        user.DeletedAt = null;
        user.DeletedBy = null;
        user.IsActive = true;

        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Restored, user, before, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<Unit> Handle(PermanentlyDeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await FindManageableAsync(RetiredUsers(), request.Id, cancellationToken);
        _currentUserService.EnsureNotSelf(user, "delete");

        var before = AccountAudit.Snapshot(user);

        // Con IsDeleted ya en true el interceptor no lo convierte en borrado lógico: el borrado es definitivo.
        user.IsDeleted = true;
        _dbContext.Users.Remove(user);
        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.PermanentlyDeleted, user, before, after: null);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    /// <summary>Cuentas en registros retirados: eliminadas lógicamente o desactivadas.</summary>
    private IQueryable<User> RetiredUsers() =>
        _dbContext.Users.IgnoreQueryFilters().Where(user => user.IsDeleted || !user.IsActive);

    private async Task<User> FindManageableAsync(IQueryable<User> users, Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        _currentUserService.EnsureCanManageRole(user.Role);

        return user;
    }
}

public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}

public class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}

public class PermanentlyDeleteUserCommandValidator : AbstractValidator<PermanentlyDeleteUserCommand>
{
    public PermanentlyDeleteUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
    }
}
