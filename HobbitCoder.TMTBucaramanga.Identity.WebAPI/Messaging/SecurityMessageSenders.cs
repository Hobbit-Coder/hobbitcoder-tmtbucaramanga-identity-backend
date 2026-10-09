using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

/// <summary>
/// Envía el código de verificación a la pasarela de mensajería: <c>POST {GatewayUrl}</c> con
/// <c>{ channel, to, template, parameters }</c>. La plantilla <c>security-code</c> la define el proveedor
/// (las de WhatsApp requieren aprobación previa); el mensaje nunca incluye contraseñas.
/// </summary>
public class HttpSecurityMessageSender : ISecurityMessageSender
{
    public const string HttpClientName = "messaging-gateway";
    private const string SendFailedMessage = "The security code could not be sent.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MessagingSettings _settings;
    private readonly ILogger<HttpSecurityMessageSender> _logger;

    public HttpSecurityMessageSender(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<HttpSecurityMessageSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.GatewayUrl)
        {
            Content = JsonContent.Create(new
            {
                channel = channel == VerificationChannel.WhatsApp ? "whatsapp" : "sms",
                to = phoneNumber,
                template = "security-code",
                parameters = new { code, purpose = purpose.ToString() }
            })
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        try
        {
            using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("La pasarela de mensajería respondió {StatusCode} al enviar un código por {Channel}.", (int)response.StatusCode, channel);
                throw new ExternalServiceException(SendFailedMessage);
            }
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalServiceException(SendFailedMessage, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException(SendFailedMessage, exception);
        }
    }
}

/// <summary>
/// Solo en desarrollo y sin pasarela configurada: escribe el código en el log para probar el primer
/// ingreso y la recuperación sin enviar mensajes reales. Nunca se registra fuera de desarrollo.
/// </summary>
public class DevelopmentSecurityMessageSender : ISecurityMessageSender
{
    private readonly ILogger<DevelopmentSecurityMessageSender> _logger;

    public DevelopmentSecurityMessageSender(ILogger<DevelopmentSecurityMessageSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        _logger.LogWarning("DESARROLLO · Código de {Purpose} por {Channel} para {PhoneNumber}: {Code}", purpose, channel, phoneNumber, code);

        return Task.CompletedTask;
    }
}

/// <summary>Fuera de desarrollo y sin pasarela: la verificación responde 503 en lugar de fingir el envío.</summary>
public class UnavailableSecurityMessageSender : ISecurityMessageSender
{
    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken) =>
        throw new ExternalServiceException("The messaging gateway is not configured.");
}
