namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }

    /// <summary>Sesión de Identity del token (claim <c>sid</c>), o <c>null</c> si no hay.</summary>
    string? SessionId { get; }
}
