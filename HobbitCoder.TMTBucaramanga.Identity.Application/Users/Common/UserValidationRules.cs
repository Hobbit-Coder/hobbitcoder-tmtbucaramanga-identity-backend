using FluentValidation;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>
/// Reglas compartidas por los comandos de usuario. La política de contraseñas no se repite aquí: la
/// aplica ASP.NET Core Identity y sus errores llegan como 400 en el campo de la contraseña.
/// </summary>
public static class UserValidationRules
{
    public const int EmailMaxLength = 256;
    public const int NameMaxLength = 100;
    public const int PhoneNumberMaxLength = 30;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Email is required")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters")
            .EmailAddress().WithMessage("Email is not valid");

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule, string fieldName) =>
        rule.NotEmpty().WithMessage($"{fieldName} is required")
            .MaximumLength(NameMaxLength).WithMessage($"{fieldName} must not exceed {NameMaxLength} characters");

    public static IRuleBuilderOptions<T, string?> ValidPhoneNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(PhoneNumberMaxLength).WithMessage($"PhoneNumber must not exceed {PhoneNumberMaxLength} characters");

    public static IRuleBuilderOptions<T, string> RequiredPassword<T>(this IRuleBuilder<T, string> rule, string fieldName = "Password") =>
        rule.NotEmpty().WithMessage($"{fieldName} is required");
}
