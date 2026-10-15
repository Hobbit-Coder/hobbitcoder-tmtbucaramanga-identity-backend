using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

/// <summary>
/// Envía el código de verificación por Evolution API (v2) autoalojado: primero confirma con
/// <c>POST {Url}/chat/whatsappNumbers/{Instance}</c> que el teléfono tiene WhatsApp y después
/// <c>POST {Url}/message/sendText/{Instance}</c>, ambos con la cabecera <c>apikey</c>. El mensaje nunca incluye
/// contraseñas. WhatsApp no envía SMS: ese canal responde 503.
/// </summary>
public class EvolutionSecurityMessageSender : ISecurityMessageSender
{
    public const string HttpClientName = "whatsapp";

    /// <summary>Milisegundos que Evolution muestra "escribiendo…" antes de enviar.</summary>
    public const int TypingDelayMilliseconds = 1000;

    private const string SendFailedMessage = "The security code could not be sent.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EvolutionSettings _settings;
    private readonly VerificationSettings _verification;
    private readonly ILogger<EvolutionSecurityMessageSender> _logger;

    public EvolutionSecurityMessageSender(
        IHttpClientFactory httpClientFactory,
        IOptions<MessagingSettings> options,
        IOptions<VerificationSettings> verification,
        ILogger<EvolutionSecurityMessageSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value.Evolution;
        _verification = verification.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string phoneNumber, VerificationChannel channel, VerificationPurpose purpose, string code, CancellationToken cancellationToken)
    {
        if (channel != VerificationChannel.WhatsApp)
        {
            throw new ExternalServiceException("SMS is not available.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var baseUrl = $"{_settings.Url!.TrimEnd('/')}";
        var instance = Uri.EscapeDataString(_settings.Instance!);
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        try
        {
            using var check = NewRequest($"{baseUrl}/chat/whatsappNumbers/{instance}", new { numbers = new[] { digits } });
            using var checkResponse = await client.SendAsync(check, cancellationToken);
            var number = await ReadNumberAsync(checkResponse, purpose, cancellationToken);

            using var send = NewRequest($"{baseUrl}/message/sendText/{instance}",
                new { number, text = Text(purpose, code, _verification.LifetimeMinutes), delay = TypingDelayMilliseconds });
            using var sendResponse = await client.SendAsync(send, cancellationToken);

            if (!sendResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Evolution API respondió {StatusCode} al enviar un código de {Purpose}: {Error}",
                    (int)sendResponse.StatusCode, purpose, await WhatsAppError.ReadAsync(sendResponse, cancellationToken));
                throw new ExternalServiceException(SendFailedMessage);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new ExternalServiceException(SendFailedMessage, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException(SendFailedMessage, exception);
        }
    }

    /// <summary>Texto del mensaje: el código en negrilla, su vigencia y el aviso de no compartirlo.</summary>
    public static string Text(VerificationPurpose purpose, string code, int lifetimeMinutes)
    {
        var action = purpose == VerificationPurpose.Activation ? "activar tu cuenta de" : "recuperar tu acceso a";

        return $"Tu código para {action} TMT Bucaramanga es *{code}*.\n\n"
            + $"Vence en {lifetimeMinutes} minutos. No lo compartas con nadie: nunca te pediremos tu contraseña.";
    }

    private HttpRequestMessage NewRequest(string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("apikey", _settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    /// <summary>Número con el que WhatsApp conoce al teléfono (la parte del <c>jid</c> antes de <c>@</c>).</summary>
    private async Task<string> ReadNumberAsync(HttpResponseMessage response, VerificationPurpose purpose, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Evolution API respondió {StatusCode} al verificar el teléfono para un código de {Purpose}: {Error}",
                (int)response.StatusCode, purpose, await WhatsAppError.ReadAsync(response, cancellationToken));
            throw new ExternalServiceException(SendFailedMessage);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var result = document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0
            ? document.RootElement[0]
            : default;

        if (result.ValueKind == JsonValueKind.Object
            && result.TryGetProperty("exists", out var exists) && exists.ValueKind == JsonValueKind.True
            && result.TryGetProperty("jid", out var jid) && jid.ValueKind == JsonValueKind.String)
        {
            return jid.GetString()!.Split('@')[0];
        }

        _logger.LogWarning("El teléfono de la cuenta no tiene WhatsApp: no se envió el código de {Purpose}.", purpose);
        throw new ExternalServiceException(SendFailedMessage);
    }
}

/// <summary>
/// Envía el código de verificación por WhatsApp Cloud API: <c>POST {BaseUrl}/{ApiVersion}/{PhoneNumberId}/messages</c>
/// con la plantilla de autenticación <see cref="MetaWhatsAppSettings.CodeTemplate"/> (el código va en el cuerpo y en el
/// botón "Copiar código"). El mensaje nunca incluye contraseñas. El proveedor no envía SMS: ese canal responde 503.
/// </summary>
public class MetaSecurityMessageSender : ISecurityMessageSender
{
    private const string SendFailedMessage = "The security code could not be sent.";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetaWhatsAppSettings _settings;
    private readonly ILogger<MetaSecurityMessageSender> _logger;

    public MetaSecurityMessageSender(IHttpClientFactory httpClientFactory, IOptions<MessagingSettings> options, ILogger<MetaSecurityMessageSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value.Meta;
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
            using var response = await _httpClientFactory.CreateClient(EvolutionSecurityMessageSender.HttpClientName).SendAsync(request, cancellationToken);

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

/// <summary>
/// Resume el error del proveedor para el log sin el cuerpo completo: <c>error.code</c> y <c>error.message</c> (Meta) o
/// <c>response.message</c> (Evolution API).
/// </summary>
public static class WhatsAppError
{
    private const string NoDetail = "sin detalle";

    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return NoDetail;
            }

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                return $"{Property(error, "code")} {Property(error, "message")}";
            }

            if (root.TryGetProperty("response", out var detail) && detail.ValueKind == JsonValueKind.Object
                && detail.TryGetProperty("message", out var message))
            {
                return message.ValueKind == JsonValueKind.Array
                    ? string.Join("; ", message.EnumerateArray().Select(item => item.ToString()))
                    : message.ToString();
            }

            return NoDetail;
        }
        catch (JsonException)
        {
            return NoDetail;
        }
    }

    private static string Property(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
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
