using FluentValidation;
using FluentValidation.Results;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValidationException = HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Account.Common;

public static class VerificationTokens
{
    public const string InvalidTokenMessage = "The verification is invalid or expired. Request a new code.";

    /// <summary>
    /// La verificación confirmada y aún sin usar del token, para el proceso indicado y con su cuenta activa.
    /// </summary>
    /// <exception cref="ValidationException">El token no es válido, ya se usó, venció o es de otro proceso.</exception>
    public static async Task<VerificationRequest> FindCompletableAsync(
        this IApplicationDbContext dbContext,
        IVerificationSecrets secrets,
        string token,
        VerificationPurpose purpose,
        DateTime now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(secrets);

        var tokenHash = secrets.ComputeHash(token);
        var verification = await dbContext.VerificationRequests
            .Include(verification => verification.User)
            .FirstOrDefaultAsync(verification => verification.ConfirmationTokenHash == tokenHash, cancellationToken);

        var isValid = verification is not null
            && verification.Purpose == purpose
            && verification.CanComplete(now)
            && verification.User.IsActive
            && (purpose == VerificationPurpose.Recovery) == (verification.User.ActivatedAt is not null);

        return isValid
            ? verification!
            : throw new ValidationException([new ValidationFailure("VerificationToken", InvalidTokenMessage)]);
    }

    /// <summary>
    /// Asigna la contraseña creada por el titular, aplicando la política de ASP.NET Core Identity antes de
    /// tocar la cuenta: una contraseña rechazada no deja cambios.
    /// </summary>
    public static async Task SetPasswordAsync(this UserManager<User> userManager, User user, string password, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(user);

        var failures = new List<ValidationFailure>();

        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            failures.AddRange(result.Errors.Select(error => new ValidationFailure(propertyName, error.Description)));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
    }

    public static IRuleBuilderOptions<T, string> ValidVerificationToken<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("VerificationToken is required")
            .MaximumLength(256).WithMessage(InvalidTokenMessage);
}
