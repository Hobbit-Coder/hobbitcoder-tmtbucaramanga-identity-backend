using HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]

namespace HobbitCoder.TMTBucaramanga.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// PostgreSQL en Docker compartido por todas las pruebas que usan base de datos. El esquema se crea
/// desde el modelo de EF (<c>EnsureCreated</c>); las pruebas usan emails únicos para no pisarse.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<IdentityContext>().UseNpgsql(ConnectionString).Options;
        await using var context = new IdentityContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
