using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
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

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// La administración actualiza una cuenta: datos del perfil, rol, estado y designación para registrar
/// asistentes. Nunca la contraseña (§6). Desactivar cierra todas las sesiones de la cuenta.
/// </summary>
/// <remarks>
/// Los tokens de acceso ya emitidos conservan el rol anterior hasta su <c>exp</c>; el siguiente refresh ya
/// trae el nuevo.
/// </remarks>
public class UpdateUserCommand : IRequest
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string UserName { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required UserRole Role { get; set; }
    public required bool IsActive { get; set; }
    public bool CanRegisterAttendees { get; set; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateUserCommandHandler(
        IApplicationDbContext dbContext,
        ILookupNormalizer normalizer,
        AuthSessionService authSessionService,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _authSessionService = authSessionService;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        _currentUserService.EnsureCanManageRole(user.Role);
        _currentUserService.EnsureCanManageRole(request.Role);

        if (user.Role != request.Role || !request.IsActive)
        {
            _currentUserService.EnsureNotSelf(user, "deactivate or change the role of");
        }

        var phoneNumber = PhoneNumbers.Normalize(request.PhoneNumber)!;
        var email = request.Email.EmptyAsNull();
        await _dbContext.EnsureIdentifiersAvailableAsync(_normalizer, request.UserName, phoneNumber, email, user.Id, cancellationToken);

        var before = AccountAudit.Snapshot(user);
        var wasActive = user.IsActive;

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Gender = request.Gender;
        user.Instagram = request.Instagram.EmptyAsNull();
        user.BirthDate = request.BirthDate;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.CanRegisterAttendees = request.CanRegisterAttendees;
        user.SetIdentifiers(_normalizer, request.UserName, phoneNumber, email);

        var action = (wasActive, user.IsActive) switch
        {
            (true, false) => AccountAuditAction.Deactivated,
            (false, true) => AccountAuditAction.Activated,
            _ => AccountAuditAction.Updated
        };

        _dbContext.Record(_currentUserService, _timeProvider, action, user, before, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (wasActive && !user.IsActive)
        {
            await _authSessionService.RevokeAllAsync(user.Id, RefreshTokenRevocationReason.UserDisabled, cancellationToken);
        }

        return Unit.Value;
    }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(command => command.FirstName).ValidName("FirstName");
        RuleFor(command => command.LastName).ValidName("LastName");
        RuleFor(command => command.Gender).IsInEnum().WithMessage("Gender is not valid");
        RuleFor(command => command.PhoneNumber).ValidPhoneNumber();
        RuleFor(command => command.Email).OptionalEmail().When(command => !string.IsNullOrWhiteSpace(command.Email));
        RuleFor(command => command.UserName).ValidUserName();
        RuleFor(command => command.Instagram).ValidInstagram();
        RuleFor(command => command.BirthDate)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("BirthDate cannot be in the future");
        RuleFor(command => command.Role).IsInEnum().WithMessage("Role is not valid");
    }
}
