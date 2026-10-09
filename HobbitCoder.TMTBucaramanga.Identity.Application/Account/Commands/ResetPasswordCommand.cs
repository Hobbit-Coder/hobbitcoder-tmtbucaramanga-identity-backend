using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;

/// <summary>
/// Recuperación del acceso (§6, HU-03): con la identidad confirmada, el titular crea una contraseña nueva.
/// Sirve también cuando la administración solicitó el reinicio. Se cierran las sesiones anteriores.
/// </summary>
public class ResetPasswordCommand : IRequest
{
    public required string VerificationToken { get; set; }
    public required string Password { get; set; }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IVerificationSecrets _secrets;
    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;
    private readonly TimeProvider _timeProvider;

    public ResetPasswordCommandHandler(
        IApplicationDbContext dbContext,
        IVerificationSecrets secrets,
        UserManager<User> userManager,
        AuthSessionService authSessionService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _secrets = secrets;
        _userManager = userManager;
        _authSessionService = authSessionService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var verification = await _dbContext.FindCompletableAsync(
            _secrets, request.VerificationToken, VerificationPurpose.Recovery, now, cancellationToken);

        await _userManager.SetPasswordAsync(verification.User, request.Password, nameof(ResetPasswordCommand.Password));
        verification.CompletedAt = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(VerificationTokens.InvalidTokenMessage, exception);
        }

        await _authSessionService.RevokeAllAsync(verification.UserId, RefreshTokenRevocationReason.PasswordChanged, cancellationToken);

        return Unit.Value;
    }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.VerificationToken).ValidVerificationToken();
        RuleFor(command => command.Password).RequiredPassword();
    }
}
