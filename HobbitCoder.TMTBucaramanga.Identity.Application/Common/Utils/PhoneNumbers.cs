namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;

/// <summary>
/// Normaliza los teléfonos para guardarlos y buscarlos igual sin importar cómo se escriban
/// ("300 000 0101", "+57 300-000-0101"): solo dígitos con prefijo <c>+</c>. Un número nacional de
/// 10 dígitos se completa con el indicativo de Colombia.
/// </summary>
public static class PhoneNumbers
{
    public const string DefaultCountryCode = "57";
    private const int NationalLength = 10;
    private const int MinDigits = 8;
    private const int MaxDigits = 15;

    /// <summary>El teléfono normalizado, o <c>null</c> si el texto no es un teléfono.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Any(character => !char.IsDigit(character) && character is not (' ' or '-' or '+' or '(' or ')' or '.')))
        {
            return null;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        if (!trimmed.StartsWith('+') && digits.Length == NationalLength)
        {
            digits = DefaultCountryCode + digits;
        }

        return digits.Length is >= MinDigits and <= MaxDigits ? $"+{digits}" : null;
    }

    public static bool IsValid(string? value) => Normalize(value) is not null;
}
