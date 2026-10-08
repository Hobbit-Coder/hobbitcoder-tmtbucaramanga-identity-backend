using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Commands;

/// <summary>Alta completa de un usuario por un administrador: estado, email confirmado y roles.</summary>
public class CreateUserCommand : IRequest<Guid>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public CreateUserCommandHandler(IApplicationDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.IsEmailTakenAsync(_userManager.NormalizeEmail(request.Email), exceptUserId: null, cancellationToken))
        {
            throw new ConflictException($"The email '{request.Email}' is already registered.");
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive,
            EmailConfirmed = request.EmailConfirmed
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        result.ThrowIfFailed();

        var roles = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (roles.Count > 0)
        {
            (await _userManager.AddToRolesAsync(user, roles)).ThrowIfFailed();
        }

        return user.Id;
    }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Password).RequiredPassword();
        RuleFor(command => command.FirstName).ValidName("FirstName");
        RuleFor(command => command.LastName).ValidName("LastName");
        RuleFor(command => command.PhoneNumber).ValidPhoneNumber();

        RuleForEach(command => command.Roles).NotEmpty().WithMessage("Role name is required");
        RuleFor(command => command.Roles)
            .MustAsync(dbContext.RolesExistAsync).WithMessage("One or more roles do not exist")
            .When(command => command.Roles.TrueForAll(role => !string.IsNullOrWhiteSpace(role)));
    }
}
