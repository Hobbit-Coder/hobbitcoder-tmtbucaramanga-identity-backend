using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;

/// <summary>
/// Emite el par de tokens de una sesión y revoca sesiones. Los roles se leen en cada emisión, así que
/// un cambio de roles se refleja en el siguiente refresh.
/// </summary>
public sealed class AuthSessionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public AuthSessionService(
        IApplicationDbContext dbContext,
        UserManager<User> userManager,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenGenerator refreshTokenGenerator,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _accessTokenIssuer = accessTokenIssuer;
        _refreshTokenGenerator = refreshTokenGenerator;
        _timeProvider = timeProvider;
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>Inicia una sesión nueva: una familia de refresh tokens nueva.</summary>
    public Task<AuthResponse> StartAsync(User user, string audience, CancellationToken cancellationToken) =>
        IssueAsync(user, audience, sessionAudience: audience, familyId: Guid.CreateVersion7(), replacedToken: null, cancellationToken);

    /// <summary>
    /// Rota <paramref name="currentToken"/>: lo revoca y emite uno nuevo en la misma sesión. El refresh
    /// token conserva la audiencia de la sesión aunque el token de acceso se pida para otra API.
    /// </summary>
    /// <exception cref="DbUpdateConcurrencyException">Otra petición ya rotó el mismo token.</exception>
    public Task<AuthResponse> RotateAsync(RefreshToken currentToken, User user, string audience, CancellationToken cancellationToken) =>
        IssueAsync(user, audience, currentToken.Audience, currentToken.FamilyId, currentToken, cancellationToken);

    /// <summary>
    /// Une otra aplicación a una sesión existente: emite un refresh token nuevo en la misma familia, con
    /// la audiencia de esa aplicación. Cerrar la sesión desde cualquiera de ellas la cierra en todas.
    /// </summary>
    public Task<AuthResponse> JoinAsync(User user, string audience, Guid familyId, CancellationToken cancellationToken) =>
        IssueAsync(user, audience, sessionAudience: audience, familyId, replacedToken: null, cancellationToken);

    /// <summary>Indica si la sesión tiene al menos un refresh token vigente (no se cerró ni expiró).</summary>
    public Task<bool> IsActiveAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var now = UtcNow;

        return _dbContext.RefreshTokens.AnyAsync(
            refreshToken => refreshToken.FamilyId == familyId && refreshToken.RevokedAt == null && refreshToken.ExpiresAt > now,
            cancellationToken);
    }

    /// <summary>Revoca de inmediato los refresh tokens activos del usuario.</summary>
    public Task RevokeAllAsync(Guid userId, RefreshTokenRevocationReason reason, CancellationToken cancellationToken) =>
        RevokeActiveAsync(_dbContext.RefreshTokens.Where(refreshToken => refreshToken.UserId == userId), reason, cancellationToken);

    /// <summary>Revoca de inmediato los refresh tokens activos de una sesión.</summary>
    public Task RevokeFamilyAsync(Guid familyId, RefreshTokenRevocationReason reason, CancellationToken cancellationToken) =>
        RevokeActiveAsync(_dbContext.RefreshTokens.Where(refreshToken => refreshToken.FamilyId == familyId), reason, cancellationToken);

    /// <summary>
    /// Un único <c>UPDATE ... WHERE RevokedAt IS NULL</c>: es idempotente y no choca con otras peticiones
    /// que revoquen o roten los mismos tokens a la vez (por ejemplo, varias que detectan la misma
    /// reutilización). Ignora el filtro de borrado lógico para revocar también los de usuarios eliminados.
    /// </summary>
    private Task RevokeActiveAsync(IQueryable<RefreshToken> tokens, RefreshTokenRevocationReason reason, CancellationToken cancellationToken)
    {
        var now = UtcNow;

        return tokens
            .IgnoreQueryFilters()
            .Where(refreshToken => refreshToken.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(refreshToken => refreshToken.RevokedAt, now)
                .SetProperty(refreshToken => refreshToken.RevocationReason, reason), cancellationToken);
    }

    private async Task<AuthResponse> IssueAsync(
        User user, string audience, string sessionAudience, Guid familyId, RefreshToken? replacedToken, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _accessTokenIssuer.Issue(new AccessTokenRequest(user.Id.ToString(), audience, [.. roles], familyId.ToString()));

        var now = _timeProvider.GetUtcNow();
        var generated = _refreshTokenGenerator.Generate();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = generated.TokenHash,
            Audience = sessionAudience,
            CreatedAt = now.UtcDateTime,
            ExpiresAt = now.Add(_refreshTokenGenerator.Lifetime).UtcDateTime
        };

        _dbContext.RefreshTokens.Add(refreshToken);

        if (replacedToken is not null)
        {
            replacedToken.Revoke(now.UtcDateTime, RefreshTokenRevocationReason.Rotated);
            replacedToken.ReplacedByTokenId = refreshToken.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            ExpiresIn = (int)Math.Max(0, (accessToken.ExpiresAt - now).TotalSeconds),
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = generated.Token,
            RefreshTokenExpiresAt = now.Add(_refreshTokenGenerator.Lifetime)
        };
    }
}
