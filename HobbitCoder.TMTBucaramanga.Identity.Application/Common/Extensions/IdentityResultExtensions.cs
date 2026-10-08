using FluentValidation.Results;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using Microsoft.AspNetCore.Identity;
using ValidationException = HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    private static readonly string[] ConflictCodes = ["DuplicateEmail", "DuplicateUserName", "DuplicateRoleName"];

    /// <summary>
    /// Traduce un <see cref="IdentityResult"/> fallido a las excepciones que la API ya sabe responder:
    /// duplicados a <see cref="ConflictException"/> (409) y el resto a <see cref="ValidationException"/>
    /// (400), agrupando los errores de contraseña bajo <paramref name="passwordPropertyName"/>.
    /// </summary>
    public static void ThrowIfFailed(this IdentityResult result, string passwordPropertyName = "Password")
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Succeeded)
        {
            return;
        }

        var conflict = result.Errors.FirstOrDefault(error => ConflictCodes.Contains(error.Code));

        if (conflict is not null)
        {
            throw new ConflictException(conflict.Description);
        }

        throw new ValidationException(result.Errors.Select(error =>
            new ValidationFailure(GetPropertyName(error.Code, passwordPropertyName), error.Description)));
    }

    private static string GetPropertyName(string code, string passwordPropertyName) => code switch
    {
        _ when code.StartsWith("Password", StringComparison.Ordinal) => passwordPropertyName,
        _ when code.Contains("Email", StringComparison.Ordinal) => "Email",
        _ when code.Contains("UserName", StringComparison.Ordinal) => "Email",
        _ when code.Contains("Role", StringComparison.Ordinal) => "Roles",
        _ => string.Empty
    };
}
