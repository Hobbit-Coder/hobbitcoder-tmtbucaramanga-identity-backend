using FluentValidation;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>
/// Reglas compartidas por los comandos de cuentas. La política de contraseñas no se repite aquí: la aplica
/// ASP.NET Core Identity y sus errores llegan como 400 en el campo de la contraseña.
/// </summary>
public static class UserValidationRules
{
    public const int EmailMaxLength = 256;
    public const int NameMaxLength = 100;
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 50;
    public const int InstagramMaxLength = 60;

    public static IRuleBuilderOptions<T, string?> OptionalEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters")
            .EmailAddress().WithMessage("Email is not valid");

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule, string fieldName) =>
        rule.NotEmpty().WithMessage($"{fieldName} is required")
            .MaximumLength(NameMaxLength).WithMessage($"{fieldName} must not exceed {NameMaxLength} characters");

    /// <summary>
    /// Nombre de usuario: letras, dígitos, punto, guion y guion bajo, con al menos una letra. Sin <c>@</c>
    /// ni solo dígitos, para no confundirlo con un correo o un teléfono al iniciar sesión.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("UserName is required")
            .Length(UserNameMinLength, UserNameMaxLength).WithMessage($"UserName must have between {UserNameMinLength} and {UserNameMaxLength} characters")
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("UserName may only contain letters, digits, '.', '_' and '-'")
            .Must(userName => userName.Any(char.IsLetter)).WithMessage("UserName must contain at least one letter");

    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("PhoneNumber is required")
            .Must(PhoneNumbers.IsValid).WithMessage("PhoneNumber is not valid");

    public static IRuleBuilderOptions<T, string?> ValidInstagram<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(InstagramMaxLength).WithMessage($"Instagram must not exceed {InstagramMaxLength} characters");

    public static IRuleBuilderOptions<T, string> RequiredPassword<T>(this IRuleBuilder<T, string> rule, string fieldName = "Password") =>
        rule.NotEmpty().WithMessage($"{fieldName} is required");
}
