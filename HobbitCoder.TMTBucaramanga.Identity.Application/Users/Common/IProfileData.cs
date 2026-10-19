using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

/// <summary>
/// Perfil que el titular completa al activar su cuenta y mantiene en "Mi perfil". Todos los datos son
/// obligatorios menos el correo (el correo es opcional; los demás datos del perfil deben quedar completos).
/// </summary>
public interface IProfileData
{
    string FirstName { get; }
    string LastName { get; }
    Gender? Gender { get; }
    string PhoneNumber { get; }
    string? Email { get; }
    string UserName { get; }
    string? Instagram { get; }
    DateOnly? BirthDate { get; }
}
