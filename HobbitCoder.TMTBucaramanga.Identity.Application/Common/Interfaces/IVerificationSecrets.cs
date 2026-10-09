namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;

/// <summary>
/// Códigos y tokens de verificación de identidad, y la política configurada para ellos (vigencia e intentos).
/// Solo se guardan sus hashes.
/// </summary>
public interface IVerificationSecrets
{
    /// <summary>Tiempo que el titular tiene para confirmar el código y crear su contraseña.</summary>
    TimeSpan Lifetime { get; }

    /// <summary>Intentos fallidos admitidos antes de invalidar la solicitud.</summary>
    int MaxAttempts { get; }

    /// <summary>Código numérico que se envía al teléfono del titular.</summary>
    string CreateCode();

    /// <summary>Token opaco que se entrega al confirmar el código.</summary>
    string CreateToken();

    string ComputeHash(string value);
}
