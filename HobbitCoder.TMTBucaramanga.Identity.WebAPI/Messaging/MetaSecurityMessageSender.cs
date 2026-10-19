using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;

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
