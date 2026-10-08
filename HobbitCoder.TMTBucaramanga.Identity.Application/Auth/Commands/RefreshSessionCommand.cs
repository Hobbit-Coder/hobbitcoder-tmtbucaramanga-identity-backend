using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Cambia un refresh token por un par de tokens nuevo. El refresh token usado queda revocado.
/// </summary>
public class RefreshSessionCommand : IRequest<AuthResponse>
{
    public required string RefreshToken { get; set; }

    /// <summary>
    /// API para el nuevo token de acceso. Si se omite, la misma con la que se inició la sesión. Permite
    /// obtener tokens para varias APIs registradas con una sola sesión.
    /// </summary>
    public string? Audience { get; set; }
}

/// <remarks>
/// Presentar un refresh token que ya se rotó significa que alguien más tiene una copia: se revoca toda la
/// sesión (la familia del token), así que tanto el atacante como el usuario legítimo deben volver a
/// iniciar sesión.
/// </remarks>
public class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthResponse>
{
    public const string InvalidRefreshTokenMessage = "The refresh token is invalid or expired.";

    private readonly IApplicationDbContext _dbContext;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<RefreshSessionCommandHandler> _logger;

    public RefreshSessionCommandHandler(
        IApplicationDbContext dbContext,
        IRefreshTokenGenerator refreshTokenGenerator,
        AuthSessionService authSessionService,
        ILogger<RefreshSessionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _refreshTokenGenerator = refreshTokenGenerator;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshSessionCommand request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.ComputeHash(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidRefreshTokenMessage);

        var now = _authSessionService.UtcNow;

        if (refreshToken.RevokedAt is not null)
        {
            if (refreshToken.RevocationReason == RefreshTokenRevocationReason.Rotated)
            {
                _logger.LogWarning("Reutilización de un refresh token ya rotado. Se revoca la sesión {FamilyId} del usuario {UserId}.",
                    refreshToken.FamilyId, refreshToken.UserId);

                await _authSessionService.RevokeFamilyAsync(refreshToken.FamilyId, RefreshTokenRevocationReason.ReuseDetected, cancellationToken);
            }

            throw new AuthenticationFailedException(InvalidRefreshTokenMessage);
        }

        if (!refreshToken.IsActive(now))
        {
            throw new AuthenticationFailedException(InvalidRefreshTokenMessage);
        }

        // La consulta aplica el filtro de borrado lógico: un usuario eliminado no aparece.
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == refreshToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await _authSessionService.RevokeFamilyAsync(refreshToken.FamilyId, RefreshTokenRevocationReason.UserDisabled, cancellationToken);

            throw new AuthenticationFailedException(InvalidRefreshTokenMessage);
        }

        try
        {
            return await _authSessionService.RotateAsync(refreshToken, user, request.Audience ?? refreshToken.Audience, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Otra petición rotó el mismo token entre la lectura y el guardado.
            throw new AuthenticationFailedException(InvalidRefreshTokenMessage, exception);
        }
    }
}

public class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator(IAccessTokenIssuer accessTokenIssuer)
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken is required")
            .MaximumLength(256).WithMessage("RefreshToken is not valid");

        RuleFor(command => command.Audience!)
            .Must(accessTokenIssuer.IsRegisteredAudience).WithMessage("Audience is not a registered API")
            .When(command => command.Audience is not null);
    }
}
