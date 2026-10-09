namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;

/// <summary>
/// El usuario está autenticado pero su rol no le permite la acción sobre ese recurso. La API la traduce a
/// un 403.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You do not have permission to perform this action.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
