using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.WebAPI;

public class EvolutionSecurityMessageSenderTests
{
    private static readonly MessagingSettings Settings = new()
    {
        Evolution = new EvolutionSettings { Url = "https://evolution.example.com/", ApiKey = "clave-de-prueba", Instance = "tmt" }
    };

    [Fact]
    public async Task ChecksTheNumber_ThenSendsTheCodeToTheReturnedNumber()
    {
        var handler = new RoutingHandler()
            .On("/chat/whatsappNumbers/tmt", HttpStatusCode.OK, """[{"jid":"573001234567@s.whatsapp.net","exists":true,"number":"573001234567"}]""")
            .On("/message/sendText/tmt", HttpStatusCode.Created, """{"key":{"id":"ABC"},"status":"PENDING"}""");

        await CreateSender(handler).SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Activation, "482913", TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.Requests.Count);
        var (check, checkBody) = handler.Requests[0];
        Assert.Equal("https://evolution.example.com/chat/whatsappNumbers/tmt", check.RequestUri!.ToString());
        Assert.Equal("clave-de-prueba", check.Headers.GetValues("apikey").Single());
        Assert.Equal("573001234567", JsonDocument.Parse(checkBody!).RootElement.GetProperty("numbers")[0].GetString());

        var (send, body) = handler.Requests[1];
        Assert.Equal(HttpMethod.Post, send.Method);
        Assert.Equal("https://evolution.example.com/message/sendText/tmt", send.RequestUri!.ToString());
        Assert.Equal("clave-de-prueba", send.Headers.GetValues("apikey").Single());
        var json = JsonDocument.Parse(body!).RootElement;
        Assert.Equal("573001234567", json.GetProperty("number").GetString());
        Assert.Equal(EvolutionSecurityMessageSender.TypingDelayMilliseconds, json.GetProperty("delay").GetInt32());
        Assert.Contains("*482913*", json.GetProperty("text").GetString());
        Assert.Contains("activar tu cuenta", json.GetProperty("text").GetString());
    }

    [Theory]
    [InlineData("""[{"jid":"573001234567@s.whatsapp.net","exists":false,"number":"573001234567"}]""")]
    [InlineData("[]")]
    public async Task NumberWithoutWhatsApp_IsNotMessaged(string checkResponse)
    {
        var handler = new RoutingHandler().On("/chat/whatsappNumbers/tmt", HttpStatusCode.OK, checkResponse);

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, """{"status":401,"error":"Unauthorized","response":{"message":"Unauthorized"}}""")]
    [InlineData(HttpStatusCode.NotFound, """{"status":404,"error":"Not Found","response":{"message":["The \"tmt\" instance does not exist"]}}""")]
    [InlineData(HttpStatusCode.OK, "no es json")]
    public async Task EvolutionFailure_IsReportedAsUnavailableService(HttpStatusCode status, string body)
    {
        var handler = new RoutingHandler().On("/chat/whatsappNumbers/tmt", status, body);

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsSms_WithoutCallingEvolution()
    {
        var handler = new RoutingHandler();

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.Sms, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Text_NamesThePurposeAndTheLifetime()
    {
        var recovery = EvolutionSecurityMessageSender.Text(VerificationPurpose.Recovery, "123456", 15);

        Assert.Contains("recuperar tu acceso", recovery);
        Assert.Contains("*123456*", recovery);
        Assert.Contains("15 minutos", recovery);
        Assert.Contains("nunca te pediremos tu contraseña", recovery);
    }

    private static EvolutionSecurityMessageSender CreateSender(RoutingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), Options.Create(new VerificationSettings()), NullLogger<EvolutionSecurityMessageSender>.Instance);
}

public class MetaSecurityMessageSenderTests
{
    private static readonly MessagingSettings Settings = new()
    {
        Provider = MessagingProvider.Meta,
        Meta = new MetaWhatsAppSettings { PhoneNumberId = "1234567890", AccessToken = "token-de-prueba" }
    };

    [Fact]
    public async Task SendsTheAuthenticationTemplate_WithTheCodeInBodyAndCopyButton()
    {
        var handler = new RoutingHandler().On("/v24.0/1234567890/messages", HttpStatusCode.OK, """{"messages":[{"id":"wamid.1"}]}""");

        await CreateSender(handler).SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Activation, "482913", TestContext.Current.CancellationToken);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("https://graph.facebook.com/v24.0/1234567890/messages", request.RequestUri!.ToString());
        Assert.Equal("token-de-prueba", request.Headers.Authorization!.Parameter);

        var json = JsonDocument.Parse(body!).RootElement;
        Assert.Equal("573001234567", json.GetProperty("to").GetString());
        var template = json.GetProperty("template");
        Assert.Equal("tmt_codigo_seguridad", template.GetProperty("name").GetString());
        var components = template.GetProperty("components");
        Assert.Equal("482913", components[0].GetProperty("parameters")[0].GetProperty("text").GetString());
        Assert.Equal("482913", components[1].GetProperty("parameters")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task ProviderError_IsReportedAsUnavailableService()
    {
        var handler = new RoutingHandler().On("/v24.0/1234567890/messages", HttpStatusCode.BadRequest, """{"error":{"code":132001,"message":"Template name does not exist in the translation"}}""");

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("""{"error":{"code":190,"message":"Invalid OAuth access token"}}""", "190 Invalid OAuth access token")]
    [InlineData("""{"status":404,"error":"Not Found","response":{"message":["Instance not found","retry"]}}""", "Instance not found; retry")]
    [InlineData("""{"status":401,"error":"Unauthorized","response":{"message":"Unauthorized"}}""", "Unauthorized")]
    [InlineData("<html>bad gateway</html>", "sin detalle")]
    [InlineData("[]", "sin detalle")]
    public async Task ProviderErrorSummary_KeepsOnlyCodeAndMessage(string body, string expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };

        Assert.Equal(expected, await WhatsAppError.ReadAsync(response, TestContext.Current.CancellationToken));
    }

    private static MetaSecurityMessageSender CreateSender(RoutingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), NullLogger<MetaSecurityMessageSender>.Instance);
}

internal sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}

/// <summary>Responde según la ruta pedida y guarda cada solicitud con su cuerpo.</summary>
internal sealed class RoutingHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = [];

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public RoutingHandler On(string path, HttpStatusCode status, string body)
    {
        _routes[path] = (status, body);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        return _routes.TryGetValue(request.RequestUri!.AbsolutePath, out var route)
            ? new HttpResponseMessage(route.Status) { Content = new StringContent(route.Body) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
