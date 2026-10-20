using HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Al arrancar, crea las cuentas de <see cref="IdentitySeed"/> si <c>Seeding:Enabled</c> está activo y la carga
/// no se ha hecho (ninguna de sus cuentas existe, ni siquiera eliminada). Nacen como las crea la administración:
/// sin contraseña, y el titular las activa en su primer ingreso. Van en un solo <c>SaveChanges</c>.
/// </summary>
/// <remarks>
/// Una cuenta cuyo teléfono o usuario ya usa otra cuenta (por ejemplo, el administrador inicial) no se crea y
/// se advierte en el log. Si falla la aplicación no arranca: se pidió la carga explícitamente.
/// </remarks>
public class IdentitySeeder : IHostedService
{
    public const string EnabledKey = "Seeding:Enabled";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<IdentitySeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>(EnabledKey))
        {
            return;
        }

        var users = IdentitySeed.Load().Users;
        var ids = users.Select(user => user.Id).ToList();

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        var normalizer = scope.ServiceProvider.GetRequiredService<ILookupNormalizer>();

        if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(user => ids.Contains(user.Id), cancellationToken))
        {
            return;
        }

        foreach (var user in users)
        {
            user.SetIdentifiers(normalizer, user.UserName!, user.PhoneNumber!, email: null);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.LockoutEnabled = true;
        }

        var phoneNumbers = users.Select(user => user.PhoneNumber).ToList();
        var userNames = users.Select(user => user.NormalizedUserName).ToList();
        var taken = await dbContext.Users.IgnoreQueryFilters()
            .Where(user => phoneNumbers.Contains(user.PhoneNumber) || userNames.Contains(user.NormalizedUserName))
            .Select(user => new { user.PhoneNumber, user.NormalizedUserName })
            .ToListAsync(cancellationToken);

        var conflicting = users.Where(user => taken.Any(existing =>
            existing.PhoneNumber == user.PhoneNumber || existing.NormalizedUserName == user.NormalizedUserName)).ToList();
        foreach (var user in conflicting)
        {
            _logger.LogWarning("Seeding: no se creó {UserName} ({UserId}): su teléfono o usuario ya pertenece a otra cuenta.", user.UserName, user.Id);
        }

        dbContext.Users.AddRange(users.Except(conflicting));
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeding: se crearon {Count} cuentas.", users.Count - conflicting.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
