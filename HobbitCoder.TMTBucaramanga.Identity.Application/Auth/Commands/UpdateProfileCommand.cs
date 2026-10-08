using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Commands;

/// <summary>
/// El usuario autenticado actualiza sus datos personales. El email, el estado y los roles solo los
/// cambia un administrador.
/// </summary>
public class UpdateProfileCommand : IRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(UpdateProfileCommand request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.FirstName).ValidName("FirstName");
        RuleFor(command => command.LastName).ValidName("LastName");
        RuleFor(command => command.PhoneNumber).ValidPhoneNumber();
    }
}
