using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Actualización completa de un usuario por un administrador. <see cref="Roles"/> reemplaza los roles
/// actuales. Desactivar al usuario cierra todas sus sesiones.
/// </summary>
/// <remarks>
/// Los tokens de acceso ya emitidos conservan los roles anteriores hasta su <c>exp</c>; el siguiente
/// refresh ya trae los nuevos.
/// </remarks>
public class UpdateUserCommand : IRequest
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public required bool IsActive { get; set; }
    public required bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(
        IApplicationDbContext dbContext,
        UserManager<User> userManager,
        AuthSessionService authSessionService,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        var requestedRoles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        EnsureAdminDoesNotLockThemselvesOut(request, requestedRoles);

        if (await _dbContext.IsEmailTakenAsync(_userManager.NormalizeEmail(request.Email), user.Id, cancellationToken))
        {
            throw new ConflictException($"The email '{request.Email}' is already registered.");
        }

        var wasActive = user.IsActive;

        user.UserName = request.Email;
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.EmailConfirmed = request.EmailConfirmed;

        (await _userManager.UpdateAsync(user)).ThrowIfFailed();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToRemove.Count > 0)
        {
            (await _userManager.RemoveFromRolesAsync(user, rolesToRemove)).ThrowIfFailed();
        }

        if (rolesToAdd.Count > 0)
        {
            (await _userManager.AddToRolesAsync(user, rolesToAdd)).ThrowIfFailed();
        }

        if (wasActive && !user.IsActive)
        {
            await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.UserDisabled, cancellationToken);
        }

        return Unit.Value;
    }

    /// <summary>Un administrador no puede desactivarse ni quitarse el rol Admin: podría quedar sin administradores.</summary>
    private void EnsureAdminDoesNotLockThemselvesOut(UpdateUserCommand request, List<string> requestedRoles)
    {
        if (request.Id != _currentUserService.GetRequiredUserId())
        {
            return;
        }

        if (!request.IsActive || !requestedRoles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ConflictException($"You cannot deactivate yourself or remove your own {RoleNames.Admin} role.");
        }
    }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.FirstName).ValidName("FirstName");
        RuleFor(command => command.LastName).ValidName("LastName");
        RuleFor(command => command.PhoneNumber).ValidPhoneNumber();

        RuleForEach(command => command.Roles).NotEmpty().WithMessage("Role name is required");
        RuleFor(command => command.Roles)
            .MustAsync(dbContext.RolesExistAsync).WithMessage("One or more roles do not exist")
            .When(command => command.Roles.TrueForAll(role => !string.IsNullOrWhiteSpace(role)));
    }
}
