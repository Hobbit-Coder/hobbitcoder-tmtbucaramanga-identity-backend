using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Inicia una sesión con un identificador de acceso (teléfono, nombre de usuario o correo registrado) y la
/// contraseña (§6).
/// </summary>
public class LoginCommand : IRequest<AuthResponse>
{
    public required string Identifier { get; set; }
    public required string Password { get; set; }

    /// <summary>API para la que se emite el token de acceso (claim <c>aud</c>).</summary>
    public required string Audience { get; set; }
}

/// <remarks>
/// Todos los fallos responden con el mismo mensaje (cuenta inexistente, sin activar, inactiva, bloqueada,
/// sin contraseña o contraseña incorrecta) para no revelar qué cuentas existen; el motivo real queda en el
/// log. Cada contraseña incorrecta suma un intento fallido, y al llegar al límite la cuenta se bloquea un
/// tiempo. Un error de ingreso nunca modifica el perfil.
/// </remarks>
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    public const string InvalidCredentialsMessage = "Invalid credentials.";

    /// <summary>
    /// Cuenta desactivada con la contraseña correcta (HU-02): se informa que el acceso no está disponible. Solo
    /// lo sabe quien ya conoce la contraseña, así que no revela qué cuentas existen.
    /// </summary>
    public const string AccountUnavailableMessage = "The account is not available.";

    private readonly IApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;
    private readonly UserManager<User> _userManager;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        ILookupNormalizer normalizer,
        UserManager<User> userManager,
        AuthSessionService authSessionService,
        ILogger<LoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _userManager = userManager;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.FindByLoginIdentifierAsync(_normalizer, request.Identifier, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Login rechazado: el identificador no corresponde a ninguna cuenta.");
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        if (!user.IsActive || user.ActivatedAt is null || user.PasswordHash is null)
        {
            _logger.LogInformation("Login rechazado: la cuenta {UserId} está inactiva, sin activar o sin contraseña.", user.Id);

            if (!user.IsActive && user.PasswordHash is not null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                throw new ForbiddenAccessException(AccountUnavailableMessage);
            }

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
        RuleFor(command => command.Identifier)
            .NotEmpty().WithMessage("Identifier is required")
            .MaximumLength(UserValidationRules.EmailMaxLength).WithMessage("Identifier is too long");
        RuleFor(command => command.Password).NotEmpty().WithMessage("Password is required");

        RuleFor(command => command.Audience)
            .NotEmpty().WithMessage("Audience is required")
            .Must(accessTokenIssuer.IsRegisteredAudience).WithMessage("Audience is not a registered API");
    }
}
