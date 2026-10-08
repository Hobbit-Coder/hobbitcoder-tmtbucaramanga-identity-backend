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

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// Un administrador asigna una contraseña nueva a un usuario sin conocer la actual. También lo
/// desbloquea y cierra todas sus sesiones.
/// </summary>
public class SetUserPasswordCommand : IRequest
{
    public required Guid Id { get; set; }
    public required string NewPassword { get; set; }
}

public class SetUserPasswordCommandHandler : IRequestHandler<SetUserPasswordCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;

    public SetUserPasswordCommandHandler(IApplicationDbContext dbContext, UserManager<User> userManager, AuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _authSessionService = authSessionService;
    }

    public async Task<Unit> Handle(SetUserPasswordCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        // Se valida antes de tocar nada: así una contraseña rechazada no deja al usuario sin contraseña.
        var failures = new List<ValidationFailure>();

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, request.NewPassword);
            failures.AddRange(result.Errors.Select(error => new ValidationFailure(nameof(SetUserPasswordCommand.NewPassword), error.Description)));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.NewPassword);
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        // Cambia el security stamp y guarda el usuario con la contraseña nueva.
        (await _userManager.UpdateSecurityStampAsync(user)).ThrowIfFailed(nameof(SetUserPasswordCommand.NewPassword));

        await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.PasswordChanged, cancellationToken);

        return Unit.Value;
    }
}

public class SetUserPasswordCommandValidator : AbstractValidator<SetUserPasswordCommand>
{
    public SetUserPasswordCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(command => command.NewPassword).RequiredPassword(nameof(SetUserPasswordCommand.NewPassword));
    }
}
