using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Registro público. Es deliberadamente limitado: el usuario queda activo, con el email sin confirmar
/// y sin roles. Los roles y el resto de datos los gestiona un administrador desde <c>/api/users</c>.
/// </summary>
public class RegisterCommand : IRequest<Guid>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public RegisterCommandHandler(IApplicationDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken = default)
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
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        result.ThrowIfFailed();

        return user.Id;
    }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email).ValidEmail();
        RuleFor(command => command.Password).RequiredPassword();
        RuleFor(command => command.FirstName).ValidName("FirstName");
        RuleFor(command => command.LastName).ValidName("LastName");
        RuleFor(command => command.PhoneNumber).ValidPhoneNumber();
    }
}
