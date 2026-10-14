using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

/// <summary>
/// Envía el código de verificación por WhatsApp Cloud API: <c>POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/messages</c>
/// con la plantilla de autenticación <see cref="MessagingSettings.CodeTemplate"/> (el código va en el cuerpo y en el
/// botón "Copiar código"). El mensaje nunca incluye contraseñas. El proveedor no envía SMS: ese canal responde 503.
/// </summary>
public class WhatsAppSecurityMessageSender : ISecurityMessageSender
{
    public const string HttpClientName = "whatsapp-cloud";
    private const string SendFailedMessage = "The security code could not be sent.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MessagingSettings _settings;
    private readonly ILogger<WhatsAppSecurityMessageSender> _logger;

    public WhatsAppSecurityMessageSender(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<WhatsAppSecurityMessageSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        if (channel != VerificationChannel.WhatsApp)
        {
            throw new ExternalServiceException("SMS is not available.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(new
            {
                messaging_product = "whatsapp",
                to = new string(phoneNumber.Where(char.IsDigit).ToArray()),
                type = "template",
                template = new
                {
                    name = _settings.CodeTemplate,
                    language = new { code = _settings.TemplateLanguage },
                    components = new object[]
                    {
                        new { type = "body", parameters = new[] { new { type = "text", text = code } } },
                        new { type = "button", sub_type = "url", index = "0", parameters = new[] { new { type = "text", text = code } } }
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

        try
        {
            using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WhatsApp Cloud API respondió {StatusCode} al enviar un código de {Purpose}: {Error}",
                    (int)response.StatusCode, purpose, await WhatsAppError.ReadAsync(response, cancellationToken));
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

/// <summary>Resume el error de la Graph API (<c>error.code</c> y <c>error.message</c>) para el log, sin el cuerpo completo.</summary>
public static class WhatsAppError
{
    private const string NoDetail = "sin detalle";

    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error)
                ? $"{Property(error, "code")} {Property(error, "message")}"
                : NoDetail;
        }
        catch (JsonException)
        {
            return NoDetail;
        }
    }

    private static string Property(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
}

/// <summary>
/// Solo en desarrollo y sin WhatsApp configurado: escribe el código en el log para probar el primer
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

/// <summary>Fuera de desarrollo y sin WhatsApp configurado: la verificación responde 503 en lugar de fingir el envío.</summary>
public class UnavailableSecurityMessageSender : ISecurityMessageSender
{
    public Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken) =>
        throw new ExternalServiceException("WhatsApp is not configured.");
}
