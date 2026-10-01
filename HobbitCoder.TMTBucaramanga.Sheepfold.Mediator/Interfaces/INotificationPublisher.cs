namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

/// <summary>
/// Envuelve un handler de notificación ya resuelto para que el publisher pueda invocarlo
/// sin conocer el tipo concreto de la notificación.
/// </summary>
/// <param name="HandlerInstance">La instancia del handler resuelta del contenedor.</param>
/// <param name="HandlerCallback">Invoca el <c>Handle</c> del handler con la notificación.</param>
public sealed record NotificationHandlerExecutor(
    object HandlerInstance,
    Func<INotification, CancellationToken, Task> HandlerCallback);

/// <summary>
/// Estrategia que define cómo se ejecutan los handlers de una notificación
/// (secuencial, en paralelo, etc.).
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Ejecuta los handlers de la notificación según la estrategia del publisher.
    /// </summary>
    /// <param name="handlerExecutors">Los handlers resueltos para la notificación.</param>
    /// <param name="notification">La notificación a publicar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken = default);
}
