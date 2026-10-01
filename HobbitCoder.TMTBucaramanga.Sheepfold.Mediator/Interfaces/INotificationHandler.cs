namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

/// <summary>
/// Define un handler que reacciona a una notificación.
/// Una misma notificación puede tener varios handlers, o ninguno.
/// </summary>
/// <typeparam name="TNotification">El tipo de notificación a procesar.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Procesa la notificación.
    /// </summary>
    /// <param name="notification">La notificación a procesar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}
