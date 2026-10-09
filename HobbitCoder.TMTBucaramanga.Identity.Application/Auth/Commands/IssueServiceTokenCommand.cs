using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Token de acceso para una aplicación de servidor (OAuth 2.0 client credentials), por ejemplo la API de
/// Sheepfold cuando consulta el directorio de cuentas. Lleva el rol <see cref="RoleNames.Service"/> y la
/// audiencia de Identity; no tiene sesión ni refresh token.
/// </summary>
public class IssueServiceTokenCommand : IRequest<ServiceTokenResponse>
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}

public sealed record ServiceTokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public class IssueServiceTokenCommandHandler : IRequestHandler<IssueServiceTokenCommand, ServiceTokenResponse>
{
    public const string IdentityAudience = "HobbitCoder.TMTBucaramanga.Identity";
    public const string InvalidClientMessage = "Invalid client credentials.";

    private readonly IServiceClientRegistry _serviceClients;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly TimeProvider _timeProvider;

    public IssueServiceTokenCommandHandler(IServiceClientRegistry serviceClients, IAccessTokenIssuer accessTokenIssuer, TimeProvider timeProvider)
    {
        _serviceClients = serviceClients;
        _accessTokenIssuer = accessTokenIssuer;
        _timeProvider = timeProvider;
    }

    public Task<ServiceTokenResponse> Handle(IssueServiceTokenCommand request, CancellationToken cancellationToken = default)
    {
        if (!_serviceClients.IsValid(request.ClientId, request.ClientSecret))
        {
            throw new AuthenticationFailedException(InvalidClientMessage);
        }

        var accessToken = _accessTokenIssuer.Issue(new AccessTokenRequest(request.ClientId, IdentityAudience, [RoleNames.Service]));
        var expiresIn = (int)Math.Max(0, (accessToken.ExpiresAt - _timeProvider.GetUtcNow()).TotalSeconds);

        return Task.FromResult(new ServiceTokenResponse(accessToken.Token, "Bearer", expiresIn));
    }
}

public class IssueServiceTokenCommandValidator : AbstractValidator<IssueServiceTokenCommand>
{
    public IssueServiceTokenCommandValidator()
    {
        RuleFor(command => command.ClientId).NotEmpty().WithMessage("ClientId is required").MaximumLength(100);
        RuleFor(command => command.ClientSecret).NotEmpty().WithMessage("ClientSecret is required").MaximumLength(512);
    }
}
