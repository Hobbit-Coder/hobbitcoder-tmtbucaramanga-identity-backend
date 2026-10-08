namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Identity contra el PostgreSQL de <see cref="PostgreSqlFixture"/>, con el primer administrador creado
/// por el bootstrap.
/// </summary>
public sealed class DatabaseApiFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin@tmtbucaramanga.test";
    public const string AdminPassword = "Admin#Bootstrap2026";

    public DatabaseApiFixture(PostgreSqlFixture database)
    {
        Factory.Settings["Persistence:Provider"] = "PostgreSQL";
        Factory.Settings["ConnectionStrings:Default"] = database.ConnectionString;
        Factory.Settings["Bootstrap:Admin:Email"] = AdminEmail;
        Factory.Settings["Bootstrap:Admin:Password"] = AdminPassword;
    }

    public IdentityApiFactory Factory { get; } = new();

    public HttpClient CreateClient() => Factory.CreateIssuerClient();

    public ValueTask InitializeAsync()
    {
        // Arranca el host: el bootstrap del administrador corre aquí.
        _ = Factory.Server;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => Factory.DisposeAsync();
}
