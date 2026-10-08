using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Mediator.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Behaviors;

/// <summary>
/// Traza inicio, fin, duración y fallo de cada comando o query.
/// Es el eslabón más externo de la cadena, para que la duración medida incluya
/// la validación y el resto de la cadena.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both",
        Justification = "El propósito de este behavior es observar la ejecución sin alterarla: registra el fallo y lo vuelve a propagar intacto")]
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var startingTimestamp = Stopwatch.GetTimestamp();

        _logger.LogInformation("Request iniciado. Request: {RequestName}, UserId: {UserId}",
            requestName, _currentUserService.UserId);

        try
        {
            var response = await next(cancellationToken);

            _logger.LogInformation("Request completado. Request: {RequestName}, Duration: {DurationMs} ms",
                requestName, Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Request fallido. Request: {RequestName}, Duration: {DurationMs} ms",
                requestName, Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds);

            throw;
        }
    }
}
