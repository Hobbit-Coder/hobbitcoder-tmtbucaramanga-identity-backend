using FluentValidation;
using FluentValidation.Results;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// El usuario autenticado cambia su contraseña. Se cierran todas sus sesiones (refresh tokens), así que
/// debe volver a iniciar sesión con la contraseña nueva.
/// </summary>
public class ChangePasswordCommand : IRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private const string PasswordMismatchCode = "PasswordMismatch";

    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(
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

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (result.Errors.Any(error => error.Code == PasswordMismatchCode))
        {
            throw new ValidationException([new ValidationFailure(
                nameof(ChangePasswordCommand.CurrentPassword), "Current password is incorrect")]);
        }

        result.ThrowIfFailed(passwordPropertyName: nameof(ChangePasswordCommand.NewPassword));

        await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.PasswordChanged, cancellationToken);

        return Unit.Value;
    }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).RequiredPassword(nameof(ChangePasswordCommand.CurrentPassword));
        RuleFor(command => command.NewPassword).RequiredPassword(nameof(ChangePasswordCommand.NewPassword));
    }
}
