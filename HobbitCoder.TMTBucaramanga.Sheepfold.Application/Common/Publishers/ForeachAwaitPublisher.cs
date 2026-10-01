using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Publishers;

/// <summary>
/// Publisher por defecto: ejecuta los handlers uno a uno, en el orden de registro,
/// esperando a que cada uno termine antes de iniciar el siguiente.
/// Si un handler falla, la excepción se propaga y los handlers restantes no se ejecutan.
/// </summary>
public sealed class ForeachAwaitPublisher : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handlerExecutors);

        foreach (var handlerExecutor in handlerExecutors)
        {
            await handlerExecutor.HandlerCallback(notification, cancellationToken);
        }
    }
}
