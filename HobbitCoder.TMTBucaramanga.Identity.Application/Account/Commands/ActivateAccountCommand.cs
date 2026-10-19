using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;

/// <summary>
/// Termina el primer ingreso: con la identidad ya confirmada, el titular crea su contraseña y
/// completa su perfil. Contraseña y perfil se guardan juntos: si algo falla, la cuenta queda como estaba (sin contraseña).
/// Después puede ingresar con su teléfono, usuario o correo si lo registró.
/// </summary>
public class ActivateAccountCommand : IRequest, IProfileData
{
    public required string VerificationToken { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required Gender? Gender { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string UserName { get; set; }
    public required string? Instagram { get; set; }
    public required DateOnly? BirthDate { get; set; }
}

public class ActivateAccountCommandHandler : IRequestHandler<ActivateAccountCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IVerificationSecrets _secrets;
    private readonly UserManager<User> _userManager;
    private readonly ILookupNormalizer _normalizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public ActivateAccountCommandHandler(
        IApplicationDbContext dbContext,
        IVerificationSecrets secrets,
        UserManager<User> userManager,
        ILookupNormalizer normalizer,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _secrets = secrets;
        _userManager = userManager;
        _normalizer = normalizer;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ActivateAccountCommand request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var verification = await _dbContext.FindCompletableAsync(
            _secrets, request.VerificationToken, VerificationPurpose.Activation, now, cancellationToken);
        var user = verification.User;

        var phoneNumber = PhoneNumbers.Normalize(request.PhoneNumber)!;
        var email = request.Email.EmptyAsNull();
        await _dbContext.EnsureIdentifiersAvailableAsync(_normalizer, request.UserName, phoneNumber, email, user.Id, cancellationToken);

        var before = AccountAudit.Snapshot(user);

        await _userManager.SetPasswordAsync(user, request.Password, nameof(ActivateAccountCommand.Password));
        user.ApplyProfile(request, _normalizer, phoneNumber, email);
        user.ActivatedAt = now;
        verification.CompletedAt = now;

        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.AccountActivated, user, before, AccountAudit.Snapshot(user));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(VerificationTokens.InvalidTokenMessage, exception);
        }

        return Unit.Value;
    }
}

public class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.VerificationToken).ValidVerificationToken();
        RuleFor(command => command.Password).RequiredPassword();
        Include(new ProfileDataValidator(timeProvider));
    }
}
