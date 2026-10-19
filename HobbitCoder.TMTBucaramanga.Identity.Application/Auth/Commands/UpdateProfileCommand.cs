using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// El usuario autenticado actualiza su perfil. El rol, el estado y la designación para
/// registrar asistentes solo los cambia la administración.
/// </summary>
public class UpdateProfileCommand : IRequest, IProfileData
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required Gender? Gender { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public required string UserName { get; set; }
    public required string? Instagram { get; set; }
    public required DateOnly? BirthDate { get; set; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateProfileCommandHandler(
        IApplicationDbContext dbContext, ILookupNormalizer normalizer, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var phoneNumber = PhoneNumbers.Normalize(request.PhoneNumber)!;
        var email = request.Email.EmptyAsNull();
        await _dbContext.EnsureIdentifiersAvailableAsync(_normalizer, request.UserName, phoneNumber, email, user.Id, cancellationToken);

        var before = AccountAudit.Snapshot(user);
        user.ApplyProfile(request, _normalizer, phoneNumber, email);

        _dbContext.Record(_currentUserService, _timeProvider, AccountAuditAction.Updated, user, before, AccountAudit.Snapshot(user));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator(TimeProvider timeProvider)
    {
        Include(new ProfileDataValidator(timeProvider));
    }
}
