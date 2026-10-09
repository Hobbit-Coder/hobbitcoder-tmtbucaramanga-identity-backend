using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>
/// La administración (director o administrador) registra una cuenta (§4, §5). Se crea <b>sin contraseña</b>:
/// el titular la activa en su primer ingreso confirmando su identidad con el teléfono o el nombre de usuario.
/// </summary>
public class CreateUserCommand : IRequest<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender? Gender { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string UserName { get; set; }
    public string? Instagram { get; set; }
    public DateOnly? BirthDate { get; set; }
    public required UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CanRegisterAttendees { get; set; }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CreateUserCommandHandler(
        IApplicationDbContext dbContext,
        ILookupNormalizer normalizer,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        _currentUserService.EnsureCanManageRole(request.Role);

        var phoneNumber = PhoneNumbers.Normalize(request.PhoneNumber)!;
        var email = request.Email.EmptyAsNull();
        await _dbContext.EnsureIdentifiersAvailableAsync(_normalizer, request.UserName, phoneNumber, email, exceptUserId: null, cancellationToken);

        // Sin contraseña: solo el titular la crea (§6).
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Gender = request.Gender,
            Instagram = request.Instagram.EmptyAsNull(),
            BirthDate = request.BirthDate,
            Role = request.Role,
            IsActive = request.IsActive,
            CanRegisterAttendees = request.CanRegisterAttendees,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            LockoutEnabled = true
        };
        user.SetIdentifiers(_normalizer, request.UserName, phoneNumber, email);

        _dbContext.Users.Add(user);
        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Created, user, before: null, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(TimeProvider timeProvider)
    {
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
