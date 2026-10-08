namespace HobbitCoder.TMTBucaramanga.Identity.Application.Auth.Common;

/// <summary>A dónde debe volver el navegador después de autorizar a una aplicación cliente.</summary>
public class AuthorizeResponse
{
    /// <summary>URI de retorno registrada del cliente, con <c>code</c> y <c>state</c> en la query.</summary>
    public required string RedirectUri { get; set; }
}
