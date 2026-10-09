using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Utils;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <remarks>
    /// Con <c>MapInboundClaims</c> activo (valor por defecto), el claim <c>sub</c> del token llega
    /// renombrado a <see cref="ClaimTypes.NameIdentifier"/>.
    /// </remarks>
    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

    /// <remarks>El mapeo de <c>MapInboundClaims</c> no incluye <c>sid</c>: llega con su nombre original.</remarks>
    public string? SessionId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sid);

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
}
