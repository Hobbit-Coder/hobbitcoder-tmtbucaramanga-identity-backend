using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// Autoriza a una aplicación cliente a entrar con la sesión del usuario autenticado en Identity: emite
/// un código de autorización de un solo uso y la URI del cliente a la que se debe volver con él.
/// </summary>
public class AuthorizeCommand : IRequest<AuthorizeResponse>
{
    public required string ClientId { get; set; }
    public required string RedirectUri { get; set; }
    public required string CodeChallenge { get; set; }
    public required string CodeChallengeMethod { get; set; }

    /// <summary>Valor opaco del cliente que vuelve sin cambios, para que compruebe que la respuesta es suya.</summary>
    public required string State { get; set; }
}

public class AuthorizeCommandHandler : IRequestHandler<AuthorizeCommand, AuthorizeResponse>
{
    public const string InactiveSessionMessage = "The session is no longer active.";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationCodeGenerator _authorizationCodeGenerator;
    private readonly AuthSessionService _authSessionService;

    public AuthorizeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuthorizationCodeGenerator authorizationCodeGenerator,
        AuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _authorizationCodeGenerator = authorizationCodeGenerator;
        _authSessionService = authSessionService;
    }

    public async Task<AuthorizeResponse> Handle(AuthorizeCommand request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var familyId = _currentUserService.GetRequiredSessionId();

        // El token de acceso sigue siendo válido hasta su exp aunque la sesión ya se haya cerrado.
        if (!await _authSessionService.IsActiveAsync(familyId, cancellationToken))
        {
            throw new AuthenticationFailedException(InactiveSessionMessage);
        }

        var now = _authSessionService.UtcNow;
        var generated = _authorizationCodeGenerator.Generate();

        _dbContext.AuthorizationCodes.Add(new AuthorizationCode
        {
            UserId = userId,
            FamilyId = familyId,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            CodeHash = generated.CodeHash,
            CodeChallenge = request.CodeChallenge,
            CreatedAt = now,
            ExpiresAt = now.Add(_authorizationCodeGenerator.Lifetime)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthorizeResponse
        {
            RedirectUri = $"{request.RedirectUri}?code={Uri.EscapeDataString(generated.Code)}&state={Uri.EscapeDataString(request.State)}"
        };
    }
}

/// <remarks>
/// El cliente y su URI de retorno se validan antes de emitir nada: un código nunca se envía a una URI
/// que no esté registrada para ese cliente.
/// </remarks>
public class AuthorizeCommandValidator : AbstractValidator<AuthorizeCommand>
{
    public AuthorizeCommandValidator(IClientRegistry clientRegistry)
    {
        RuleFor(command => command.ClientId)
            .NotEmpty().WithMessage("ClientId is required")
            .Must(clientId => clientRegistry.FindClient(clientId) is not null).WithMessage("ClientId is not a registered client");

        RuleFor(command => command.RedirectUri)
            .NotEmpty().WithMessage("RedirectUri is required")
            .Must((command, redirectUri) => clientRegistry.FindClient(command.ClientId)?.AllowsRedirectUri(redirectUri) == true)
            .WithMessage("RedirectUri is not registered for this client");

        RuleFor(command => command.CodeChallenge)
            .NotEmpty().WithMessage("CodeChallenge is required")
            .Matches(Pkce.CodeChallengePattern).WithMessage("CodeChallenge must be a base64url SHA-256 hash");

        RuleFor(command => command.CodeChallengeMethod)
            .Equal(Pkce.S256).WithMessage($"CodeChallengeMethod must be {Pkce.S256}");

        RuleFor(command => command.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(512).WithMessage("State is too long");
    }
}
