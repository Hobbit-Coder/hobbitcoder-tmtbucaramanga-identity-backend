namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;

/// <summary>
/// Datos iniciales que se crean al arrancar, leídos de la sección <see cref="SectionName"/>. Solo se
/// usan si están configurados, y nunca van en <c>appsettings*.json</c>: se pasan por user-secrets o
/// variables de entorno.
/// </summary>
public class BootstrapSettings
{
    public const string SectionName = "Bootstrap";

    public BootstrapAdminSettings Admin { get; set; } = new();
}

/// <summary>
/// Primer administrador. Se crea solo si <see cref="UserName"/> está configurado y todavía no hay ninguna
/// cuenta con el rol Administrador.
/// </summary>
public class BootstrapAdminSettings
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string FirstName { get; set; } = "Administrador";
    public string LastName { get; set; } = "TMT Bucaramanga";
}
