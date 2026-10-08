namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;

/// <summary>
/// Credenciales o refresh token inválidos. El manejador de excepciones de la API la traduce a un 401
/// con su mensaje, que nunca dice cuál dato falló (usuario inexistente, contraseña, cuenta inactiva
/// o bloqueada) para no revelar qué cuentas existen.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base()
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
