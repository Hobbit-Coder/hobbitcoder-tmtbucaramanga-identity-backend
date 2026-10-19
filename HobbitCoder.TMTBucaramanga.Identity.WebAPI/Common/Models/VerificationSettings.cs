namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// Política de los códigos de seguridad (sección <see cref="SectionName"/>). La longitud, la vigencia y los intentos
/// son valores configurables, con estos valores por defecto.
/// </summary>
public class VerificationSettings
{
    public const string SectionName = "Verification";

    public int CodeLength { get; set; } = 6;
    public int LifetimeMinutes { get; set; } = 15;
    public int MaxAttempts { get; set; } = 5;
}
