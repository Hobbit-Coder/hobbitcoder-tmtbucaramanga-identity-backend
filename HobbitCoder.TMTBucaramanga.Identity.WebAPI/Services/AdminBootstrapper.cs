using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Contexts;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Services;

/// <summary>
/// Al arrancar, crea el primer administrador configurado en <see cref="BootstrapSettings"/> si todavía no
/// existe ninguno. Es la única cuenta que nace activada y con contraseña: una base nueva no tiene a nadie
/// que registre cuentas. Es idempotente; sin <c>Bootstrap:Admin:UserName</c> no toca la base de datos.
/// </summary>
/// <remarks>
/// Si falla (base de datos inaccesible, datos o contraseña inválidos) la aplicación no arranca: se pidió un
/// administrador explícitamente y no tenerlo dejaría la administración de cuentas inutilizable. Varias
/// instancias pueden arrancar a la vez: ante un fallo se comprueba si otra ya lo creó y se reintenta.
/// </remarks>
public class AdminBootstrapper : IHostedService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BootstrapAdminSettings _settings;
    private readonly ILogger<AdminBootstrapper> _logger;

    public AdminBootstrapper(IServiceScopeFactory scopeFactory, IOptions<BootstrapSettings> options, ILogger<AdminBootstrapper> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value.Admin;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.UserName))
        {
            return;
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await EnsureAdminAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
            {
                if (await AdminExistsAsync(cancellationToken))
                {
                    _logger.LogInformation(exception, "Bootstrap: otra instancia creó el administrador al mismo tiempo.");
                    return;
                }

                if (attempt == MaxAttempts)
                {
                    throw;
                }

                _logger.LogInformation(exception, "Bootstrap: falló el intento {Attempt}; se reintenta.", attempt);
                await Task.Delay(RetryDelay * attempt, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<bool> AdminExistsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityContext>();

        return await dbContext.Users.IgnoreQueryFilters().AnyAsync(user => user.Role == UserRole.Admin, cancellationToken);
    }

    private async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (await AdminExistsAsync(cancellationToken))
        {
            return;
        }

        var phoneNumber = PhoneNumbers.Normalize(_settings.PhoneNumber)
            ?? throw new InvalidOperationException($"{BootstrapSettings.SectionName}:Admin:PhoneNumber es obligatorio y debe ser un teléfono válido.");

        if (string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException($"{BootstrapSettings.SectionName}:Admin:Password es obligatorio para crear el administrador.");
        }

        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var admin = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = _settings.UserName,
            PhoneNumber = phoneNumber,
            Email = string.IsNullOrWhiteSpace(_settings.Email) ? null : _settings.Email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(_settings.Email),
            FirstName = _settings.FirstName,
            LastName = _settings.LastName,
            Role = UserRole.Admin,
            IsActive = true,
            ActivatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, _settings.Password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Bootstrap: no se pudo crear el administrador: {string.Join(" ", result.Errors.Select(error => error.Description))}");
        }

        _logger.LogInformation("Bootstrap: se creó el administrador {UserId}.", admin.Id);
    }
}
