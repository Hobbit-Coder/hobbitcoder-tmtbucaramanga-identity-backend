using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Queries;

/// <summary>
/// Datos que la administración ya registró para la cuenta en activación, para que el titular complete solo
/// lo que falta sin volver a escribir lo confirmado. Exige el token de verificación vigente.
/// </summary>
public class GetActivationProfileQuery : IRequest<UserResponse>
{
    public required string VerificationToken { get; set; }
}

public class GetActivationProfileQueryHandler : IRequestHandler<GetActivationProfileQuery, UserResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IVerificationSecrets _secrets;
    private readonly TimeProvider _timeProvider;

    public GetActivationProfileQueryHandler(IApplicationDbContext dbContext, IVerificationSecrets secrets, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _secrets = secrets;
        _timeProvider = timeProvider;
    }

    public async Task<UserResponse> Handle(GetActivationProfileQuery request, CancellationToken cancellationToken = default)
    {
        var verification = await _dbContext.FindCompletableAsync(
            _secrets, request.VerificationToken, VerificationPurpose.Activation, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return UserResponse.From(verification.User, LocalCalendar.Today(_timeProvider));
    }
}

public class GetActivationProfileQueryValidator : AbstractValidator<GetActivationProfileQuery>
{
    public GetActivationProfileQueryValidator()
    {
        RuleFor(query => query.VerificationToken).ValidVerificationToken();
    }
}
