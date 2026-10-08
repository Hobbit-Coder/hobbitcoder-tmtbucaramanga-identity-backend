namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;

/// <summary>
/// Se lanza cuando la petición es válida en forma pero choca con el estado actual de los datos
/// (un destino duplicado, el último puerto activo de un destino). El manejador de excepciones de
/// la API la traduce a un 409 con el motivo, en lugar de dejar salir el error crudo de PostgreSQL.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException()
        : base()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
