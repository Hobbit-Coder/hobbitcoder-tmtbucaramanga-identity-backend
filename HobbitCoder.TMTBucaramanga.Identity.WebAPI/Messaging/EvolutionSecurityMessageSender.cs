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
