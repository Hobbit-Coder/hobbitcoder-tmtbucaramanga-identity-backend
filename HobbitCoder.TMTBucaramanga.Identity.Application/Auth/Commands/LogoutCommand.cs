using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Cierra la sesión del refresh token: revoca todos los de su familia. Basta con tener el refresh token.
/// </summary>
/// <remarks>
/// Es idempotente y no revela si el token existía: un token desconocido o ya revocado también responde
/// bien. Los tokens de acceso ya emitidos siguen siendo válidos hasta su <c>exp</c>.
/// </remarks>
public class LogoutCommand : IRequest
{
    public required string RefreshToken { get; set; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly AuthSessionService _authSessionService;

    public LogoutCommandHandler(IApplicationDbContext dbContext, IRefreshTokenGenerator refreshTokenGenerator, AuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _refreshTokenGenerator = refreshTokenGenerator;
        _authSessionService = authSessionService;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.ComputeHash(request.RefreshToken);
        var familyId = await _dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is not null)
        {
            await _authSessionService.RevokeFamilyAsync(familyId.Value, RefreshTokenRevocationReason.Logout, cancellationToken);
        }

        return Unit.Value;
    }
}

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken is required")
            .MaximumLength(256).WithMessage("RefreshToken is not valid");
    }
}
