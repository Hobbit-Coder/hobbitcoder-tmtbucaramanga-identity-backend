namespace HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;

/// <summary>
/// Define el contrato para el mediador que envía requests a sus handlers correspondientes
/// y publica notificaciones a todos sus handlers.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Envía un request al handler correspondiente y devuelve la respuesta.
    /// </summary>
    /// <typeparam name="TRequest">El tipo de request (comando o query).</typeparam>
    /// <typeparam name="TResponse">El tipo de respuesta esperada.</typeparam>
    /// <param name="request">El request a enviar (comando o query).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>La respuesta producida por el handler.</returns>
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;

    /// <summary>
    /// Publica una notificación a todos sus handlers registrados.
    /// Si no hay handlers, la publicación no hace nada.
    /// </summary>
    /// <typeparam name="TNotification">El tipo de notificación.</typeparam>
    /// <param name="notification">La notificación a publicar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
