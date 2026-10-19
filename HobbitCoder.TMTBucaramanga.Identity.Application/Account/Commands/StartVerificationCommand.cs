using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;

/// <summary>
/// El titular pide un código para confirmar su identidad: para activar su cuenta (primer ingreso) o
/// para recuperar el acceso / crear la contraseña después de un reinicio. El código se envía solo al
/// teléfono registrado de la cuenta, por SMS o WhatsApp.
/// </summary>
/// <remarks>
/// La respuesta es la misma exista o no la cuenta y corresponda o no el proceso (por ejemplo, activar una
/// cuenta ya activa): así no revela qué cuentas existen. En esos casos no se envía nada y el código que
/// se ingrese después será rechazado.
/// </remarks>
public class StartVerificationCommand : IRequest<StartVerificationResponse>
{
    /// <summary>Teléfono o nombre de usuario (el correo no sirve para el primer ingreso).</summary>
    public required string Identifier { get; set; }

    public required VerificationChannel Channel { get; set; }
    public required VerificationPurpose Purpose { get; set; }
}

public class StartVerificationCommandHandler : IRequestHandler<StartVerificationCommand, StartVerificationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;
    private readonly IVerificationSecrets _secrets;
    private readonly ISecurityMessageSender _messageSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StartVerificationCommandHandler> _logger;

    public StartVerificationCommandHandler(
        IApplicationDbContext dbContext,
        ILookupNormalizer normalizer,
        IVerificationSecrets secrets,
        ISecurityMessageSender messageSender,
        TimeProvider timeProvider,
        ILogger<StartVerificationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _secrets = secrets;
        _messageSender = messageSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<StartVerificationResponse> Handle(StartVerificationCommand request, CancellationToken cancellationToken = default)
    {
        var user = request.Identifier.Contains('@', StringComparison.Ordinal)
            ? null
            : await _dbContext.FindByLoginIdentifierAsync(_normalizer, request.Identifier, cancellationToken);

        if (user is null || !CanStart(user, request.Purpose))
        {
            _logger.LogInformation("Verificación no iniciada: el identificador no corresponde a una cuenta para el proceso {Purpose}.", request.Purpose);
            return new StartVerificationResponse { VerificationId = Guid.CreateVersion7() };
        }

        var code = _secrets.CreateCode();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var verification = new VerificationRequest
        {
            UserId = user.Id,
            Purpose = request.Purpose,
            Channel = request.Channel,
            CodeHash = _secrets.ComputeHash(code),
            CreatedAt = now,
            ExpiresAt = now.Add(_secrets.Lifetime)
        };

        _dbContext.VerificationRequests.Add(verification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _messageSender.SendVerificationCodeAsync(user.PhoneNumber!, request.Channel, request.Purpose, code, cancellationToken);

        return new StartVerificationResponse { VerificationId = verification.Id };
    }

    /// <summary>
    /// Activación: cuenta activa que aún no completa el primer ingreso. Recuperación: cuenta activa ya
    /// activada (incluye la que tiene un reinicio solicitado). Ambas requieren teléfono registrado.
    /// </summary>
    private static bool CanStart(User user, VerificationPurpose purpose) =>
        user.IsActive && user.PhoneNumber is not null && purpose switch
        {
            VerificationPurpose.Activation => user.ActivatedAt is null,
            VerificationPurpose.Recovery => user.ActivatedAt is not null,
            _ => false
        };
}

public class StartVerificationCommandValidator : AbstractValidator<StartVerificationCommand>
{
    public StartVerificationCommandValidator()
    {
        RuleFor(command => command.Identifier)
            .NotEmpty().WithMessage("Identifier is required")
            .MaximumLength(UserValidationRules.EmailMaxLength).WithMessage("Identifier is too long");
        RuleFor(command => command.Channel).IsInEnum().WithMessage("Channel is not valid");
        RuleFor(command => command.Purpose).IsInEnum().WithMessage("Purpose is not valid");
    }
}
