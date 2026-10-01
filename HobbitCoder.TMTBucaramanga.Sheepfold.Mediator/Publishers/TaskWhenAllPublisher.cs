using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Publishers;

/// <summary>
/// Ejecuta todos los handlers de forma concurrente y espera a que terminen todos.
/// Un fallo no detiene a los demás handlers; al final se propaga la primera excepción.
/// </summary>
/// <remarks>
/// Los handlers comparten el scope de la petición: no usar este publisher si más de un
/// handler accede al mismo DbContext, porque el DbContext no admite operaciones concurrentes.
/// </remarks>
public sealed class TaskWhenAllPublisher : INotificationPublisher
{
    public Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handlerExecutors);

        return Task.WhenAll(handlerExecutors.Select(handlerExecutor =>
            handlerExecutor.HandlerCallback(notification, cancellationToken)));
    }
}
