using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace HobbitCoder.TMTBucaramanga.Identity.UnitTests.WebAPI;

public class WhatsAppSecurityMessageSenderTests
{
    private static readonly MessagingSettings Settings = new()
    {
        PhoneNumberId = "1234567890",
        AccessToken = "token-de-prueba",
        CodeTemplate = "tmt_codigo_seguridad",
        TemplateLanguage = "es"
    };

    [Fact]
    public async Task SendsTheAuthenticationTemplate_WithTheCodeInBodyAndCopyButton()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.1"}]}""");

        await CreateSender(handler).SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Activation, "482913", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://graph.facebook.com/v24.0/1234567890/messages", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("token-de-prueba", handler.Request.Headers.Authorization.Parameter);

        var body = JsonDocument.Parse(handler.Body!).RootElement;
        Assert.Equal("whatsapp", body.GetProperty("messaging_product").GetString());
        Assert.Equal("573001234567", body.GetProperty("to").GetString());
        var template = body.GetProperty("template");
        Assert.Equal("tmt_codigo_seguridad", template.GetProperty("name").GetString());
        Assert.Equal("es", template.GetProperty("language").GetProperty("code").GetString());
        var components = template.GetProperty("components");
        Assert.Equal("482913", components[0].GetProperty("parameters")[0].GetProperty("text").GetString());
        Assert.Equal("url", components[1].GetProperty("sub_type").GetString());
        Assert.Equal("482913", components[1].GetProperty("parameters")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task RejectsSms_WithoutCallingTheProvider()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.Sms, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));

        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task ProviderError_IsReportedAsUnavailableService()
    {
        var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"error":{"code":132001,"message":"Template name does not exist in the translation"}}""");

        await Assert.ThrowsAsync<ExternalServiceException>(() => CreateSender(handler)
            .SendVerificationCodeAsync("+573001234567", VerificationChannel.WhatsApp, VerificationPurpose.Recovery, "482913", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("""{"error":{"code":190,"message":"Invalid OAuth access token"}}""", "190 Invalid OAuth access token")]
    [InlineData("<html>bad gateway</html>", "sin detalle")]
    [InlineData("[]", "sin detalle")]
    public async Task ProviderErrorSummary_KeepsOnlyCodeAndMessage(string body, string expected)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(body) };

        Assert.Equal(expected, await WhatsAppError.ReadAsync(response, TestContext.Current.CancellationToken));
    }

    private static WhatsAppSecurityMessageSender CreateSender(RecordingHandler handler) =>
        new(new SingleClientFactory(handler), Options.Create(Settings), NullLogger<WhatsAppSecurityMessageSender>.Instance);

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
