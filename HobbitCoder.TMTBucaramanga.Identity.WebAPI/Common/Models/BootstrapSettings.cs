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
/// Primer administrador. Se crea solo si <see cref="Email"/> está configurado y todavía no hay ningún
/// usuario con el rol Admin; si ya existe una cuenta con ese email, solo se le asigna el rol.
/// </summary>
public class BootstrapAdminSettings
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FirstName { get; set; } = "Administrador";
    public string LastName { get; set; } = "TMT Bucaramanga";
}
