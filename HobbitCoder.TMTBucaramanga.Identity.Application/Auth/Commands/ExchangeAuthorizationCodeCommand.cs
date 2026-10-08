using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Canjea un código de autorización por un par de tokens para el cliente. Los tokens pertenecen a la
/// misma sesión de Identity en la que se emitió el código.
/// </summary>
public class ExchangeAuthorizationCodeCommand : IRequest<AuthResponse>
{
    public required string Code { get; set; }
    public required string CodeVerifier { get; set; }
    public required string ClientId { get; set; }
    public required string RedirectUri { get; set; }
}

/// <remarks>
/// El código se marca como usado en el primer intento, aunque falle, para que no se pueda probar más de
/// un <c>code_verifier</c>. Presentar un código ya usado significa que alguien más lo tiene: se revoca
/// toda la sesión, igual que con un refresh token reutilizado. Todos los fallos responden con el mismo
/// mensaje; el motivo real queda en el log.
/// </remarks>
public class ExchangeAuthorizationCodeCommandHandler : IRequestHandler<ExchangeAuthorizationCodeCommand, AuthResponse>
{
    public const string InvalidCodeMessage = "The authorization code is invalid or expired.";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAuthorizationCodeGenerator _authorizationCodeGenerator;
    private readonly IClientRegistry _clientRegistry;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<ExchangeAuthorizationCodeCommandHandler> _logger;

    public ExchangeAuthorizationCodeCommandHandler(
        IApplicationDbContext dbContext,
        IAuthorizationCodeGenerator authorizationCodeGenerator,
        IClientRegistry clientRegistry,
        AuthSessionService authSessionService,
        ILogger<ExchangeAuthorizationCodeCommandHandler> logger)
    {
        _dbContext = dbContext;
        _authorizationCodeGenerator = authorizationCodeGenerator;
        _clientRegistry = clientRegistry;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(ExchangeAuthorizationCodeCommand request, CancellationToken cancellationToken = default)
    {
        var codeHash = _authorizationCodeGenerator.ComputeHash(request.Code);
        var authorizationCode = await _dbContext.AuthorizationCodes
            .FirstOrDefaultAsync(code => code.CodeHash == codeHash, cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidCodeMessage);

        var now = _authSessionService.UtcNow;

        if (authorizationCode.ConsumedAt is not null)
        {
            _logger.LogWarning("Reutilización de un código de autorización. Se revoca la sesión {FamilyId} del usuario {UserId}.",
                authorizationCode.FamilyId, authorizationCode.UserId);

            await _authSessionService.RevokeFamilyAsync(authorizationCode.FamilyId, RefreshTokenRevocationReason.ReuseDetected, cancellationToken);

            throw new AuthenticationFailedException(InvalidCodeMessage);
        }

        await ConsumeAsync(authorizationCode, now, cancellationToken);

        var rejection = GetRejectionReason(authorizationCode, request, now);

        if (rejection is not null)
        {
            _logger.LogInformation("Canje de código rechazado para el cliente {ClientId}: {Reason}.", request.ClientId, rejection);
            throw new AuthenticationFailedException(InvalidCodeMessage);
        }

        // El cliente pudo quitarse de la configuración después de emitir el código.
        var client = _clientRegistry.FindClient(authorizationCode.ClientId)
            ?? throw new AuthenticationFailedException(InvalidCodeMessage);
        var user = await FindActiveUserAsync(authorizationCode, cancellationToken);

        return await _authSessionService.JoinAsync(user, client.Audience, authorizationCode.FamilyId, cancellationToken);
    }

    private async Task ConsumeAsync(AuthorizationCode authorizationCode, DateTime now, CancellationToken cancellationToken)
    {
        authorizationCode.Consume(now);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Otra petición canjeó el mismo código entre la lectura y el guardado.
            throw new AuthenticationFailedException(InvalidCodeMessage, exception);
        }
    }

    private static string? GetRejectionReason(AuthorizationCode authorizationCode, ExchangeAuthorizationCodeCommand request, DateTime now)
    {
        if (authorizationCode.ExpiresAt <= now)
        {
            return "el código expiró";
        }

        if (!string.Equals(authorizationCode.ClientId, request.ClientId, StringComparison.Ordinal))
        {
            return "el código se emitió para otro cliente";
        }

        if (!string.Equals(authorizationCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
        {
            return "la URI de retorno no coincide";
        }

        return Pkce.IsValid(request.CodeVerifier, authorizationCode.CodeChallenge) ? null : "el code_verifier no cumple el desafío PKCE";
    }

    /// <summary>La sesión debe seguir abierta y el usuario activo; la consulta aplica el filtro de borrado lógico.</summary>
    private async Task<User> FindActiveUserAsync(AuthorizationCode authorizationCode, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == authorizationCode.UserId, cancellationToken);

        if (user is null || !user.IsActive || !await _authSessionService.IsActiveAsync(authorizationCode.FamilyId, cancellationToken))
        {
            throw new AuthenticationFailedException(InvalidCodeMessage);
        }

        return user;
    }
}

public class ExchangeAuthorizationCodeCommandValidator : AbstractValidator<ExchangeAuthorizationCodeCommand>
{
    public ExchangeAuthorizationCodeCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(256).WithMessage("Code is not valid");

        RuleFor(command => command.CodeVerifier)
            .NotEmpty().WithMessage("CodeVerifier is required")
            .Matches(Pkce.CodeVerifierPattern).WithMessage("CodeVerifier must have 43 to 128 unreserved characters");

        RuleFor(command => command.ClientId).NotEmpty().WithMessage("ClientId is required");
        RuleFor(command => command.RedirectUri).NotEmpty().WithMessage("RedirectUri is required");
    }
}
