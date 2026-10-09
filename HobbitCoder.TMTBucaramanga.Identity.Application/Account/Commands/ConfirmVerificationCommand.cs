using FluentValidation;
using FluentValidation.Results;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ValidationException = HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Commands;

/// <summary>
/// El titular confirma su identidad con el código recibido. Entrega un token de verificación de un solo
/// uso para crear la contraseña (y, en la activación, completar el perfil). Un código inválido no cambia
/// nada de la cuenta.
/// </summary>
public class ConfirmVerificationCommand : IRequest<ConfirmVerificationResponse>
{
    public required Guid VerificationId { get; set; }
    public required string Code { get; set; }
}

public sealed record ConfirmVerificationResponse(string VerificationToken);

public class ConfirmVerificationCommandHandler : IRequestHandler<ConfirmVerificationCommand, ConfirmVerificationResponse>
{
    public const string InvalidCodeMessage = "The code is invalid or expired.";

    private readonly IApplicationDbContext _dbContext;
    private readonly IVerificationSecrets _secrets;
    private readonly TimeProvider _timeProvider;

    public ConfirmVerificationCommandHandler(IApplicationDbContext dbContext, IVerificationSecrets secrets, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _secrets = secrets;
        _timeProvider = timeProvider;
    }

    public async Task<ConfirmVerificationResponse> Handle(ConfirmVerificationCommand request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var verification = await _dbContext.VerificationRequests
            .FirstOrDefaultAsync(verification => verification.Id == request.VerificationId && verification.User.IsActive, cancellationToken);

        if (verification is null || !verification.CanConfirm(now, _secrets.MaxAttempts))
        {
            throw InvalidCode();
        }

        try
        {
            if (!HashesMatch(verification.CodeHash, _secrets.ComputeHash(request.Code.Trim())))
            {
                verification.FailedAttempts++;
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw InvalidCode();
            }

            var token = _secrets.CreateToken();
            verification.ConfirmedAt = now;
            verification.ConfirmationTokenHash = _secrets.ComputeHash(token);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ConfirmVerificationResponse(token);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Otra petición confirmó o falló con la misma solicitud al mismo tiempo.
            throw new ValidationException(InvalidCodeMessage, exception);
        }
    }

    private static bool HashesMatch(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));

    private static ValidationException InvalidCode() =>
        new([new ValidationFailure(nameof(ConfirmVerificationCommand.Code), InvalidCodeMessage)]);
}

public class ConfirmVerificationCommandValidator : AbstractValidator<ConfirmVerificationCommand>
{
    public ConfirmVerificationCommandValidator()
    {
        RuleFor(command => command.VerificationId).NotEmpty().WithMessage("VerificationId is required");
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(20).WithMessage(ConfirmVerificationCommandHandler.InvalidCodeMessage);
    }
}
