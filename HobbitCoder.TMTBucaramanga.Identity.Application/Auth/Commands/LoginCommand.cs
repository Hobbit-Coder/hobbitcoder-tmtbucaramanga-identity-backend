using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>Inicia una sesión con email y contraseña.</summary>
public class LoginCommand : IRequest<AuthResponse>
{
    public required string Email { get; set; }
    public required string Password { get; set; }

    /// <summary>API para la que se emite el token de acceso (claim <c>aud</c>).</summary>
    public required string Audience { get; set; }
}

/// <remarks>
/// Todos los fallos responden con el mismo mensaje (usuario inexistente, contraseña incorrecta, cuenta
/// inactiva o bloqueada) para no revelar qué cuentas existen; el motivo real queda en el log. Cada
/// contraseña incorrecta suma un intento fallido, y al llegar al límite la cuenta se bloquea un tiempo.
/// </remarks>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    public const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(UserManager<User> userManager, AuthSessionService authSessionService, ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogInformation("Login rechazado: no existe un usuario con ese email.");
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Login rechazado: el usuario {UserId} está inactivo.", user.Id);
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login rechazado: el usuario {UserId} está bloqueado hasta {LockoutEnd}.", user.Id, user.LockoutEnd);
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogInformation("Login rechazado: contraseña incorrecta para el usuario {UserId}.", user.Id);
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return await _authSessionService.StartAsync(user, request.Audience, cancellationToken);
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(IAccessTokenIssuer accessTokenIssuer)
    {
        RuleFor(command => command.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(command => command.Password).NotEmpty().WithMessage("Password is required");

        RuleFor(command => command.Audience)
            .NotEmpty().WithMessage("Audience is required")
            .Must(accessTokenIssuer.IsRegisteredAudience).WithMessage("Audience is not a registered API");
    }
}
