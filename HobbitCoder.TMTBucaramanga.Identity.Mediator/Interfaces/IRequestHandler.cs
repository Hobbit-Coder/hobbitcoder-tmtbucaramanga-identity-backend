namespace HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

/// <summary>
/// Define un handler para procesar un request y producir una respuesta.
/// Los handlers implementan la lógica de negocio para comandos y queries.
/// </summary>
/// <typeparam name="TRequest">El tipo de request a procesar (comando o query).</typeparam>
/// <typeparam name="TResponse">El tipo de respuesta que produce el handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Procesa el request y produce una respuesta.
    /// </summary>
    /// <param name="request">El request a procesar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La respuesta producida por el handler.</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
