using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;

/// <summary>
/// Interfaz marcadora para comandos y queries que producen una respuesta.
/// Los comandos/queries implementan esta interfaz para indicar qué tipo de respuesta producen.
/// </summary>
/// <typeparam name="TResponse">El tipo de respuesta que produce el request.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed",
    Justification = "TResponse es usado como marcador de tipo para inferencia en el mediator")]
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Interfaz marcadora para comandos y queries que no producen respuesta (void).
/// </summary>
public interface IRequest : IRequest<Unit>
{
}
