namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

/// <summary>
/// Representa el siguiente eslabón de la cadena: el behavior que sigue o, al final, el handler.
/// </summary>
/// <typeparam name="TResponse">El tipo de respuesta que produce el request.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Comportamiento transversal que envuelve la ejecución de un request.
/// El orden de registro en el contenedor define el orden de ejecución: el primero
/// registrado es el más externo.
/// </summary>
/// <typeparam name="TRequest">El tipo de request (comando o query).</typeparam>
/// <typeparam name="TResponse">El tipo de respuesta esperada.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Ejecuta el comportamiento e invoca <paramref name="next"/> para continuar la cadena.
    /// </summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default);
}
