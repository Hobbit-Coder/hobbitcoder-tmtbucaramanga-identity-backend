using HobbitCoder.TMTBucaramanga.Identity.Domain.Constants;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Identity.WebAPI.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HobbitCoder.TMTBucaramanga.Identity.WebAPI.Services;

/// <summary>
/// Al arrancar, crea el rol <see cref="RoleNames.Admin"/> y el primer administrador configurado en
/// <see cref="BootstrapSettings"/>. Es idempotente: si ya hay algún administrador no hace nada, así que
/// la configuración puede quedarse puesta. Sin <c>Bootstrap:Admin:Email</c> no toca la base de datos.
/// </summary>
/// <remarks>
/// Si falla (base de datos inaccesible, contraseña que no cumple la política) la aplicación no arranca:
/// se pidió un administrador explícitamente y no tenerlo dejaría <c>/api/users</c> inutilizable.
/// <para>
/// Varias instancias pueden arrancar a la vez y competir por crear el rol, el usuario o la asignación.
/// Ante un fallo se comprueba con un scope nuevo si otra instancia ya dejó un administrador; si no, se
/// espera un poco y se repite el proceso, que salta los pasos ya hechos.
/// </para>
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
        if (string.IsNullOrWhiteSpace(_settings.Email))
        {
            return;
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await EnsureAdminAsync(_settings.Email);
                return;
            }
            catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
            {
                if (await AdminExistsAsync())
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

    private async Task<bool> AdminExistsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        return (await userManager.GetUsersInRoleAsync(RoleNames.Admin)).Count > 0;
    }

    private async Task EnsureAdminAsync(string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

        if ((await userManager.GetUsersInRoleAsync(RoleNames.Admin)).Count > 0)
        {
            return;
        }

        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            EnsureSucceeded(await roleManager.CreateAsync(new Role { Name = RoleNames.Admin }), $"crear el rol {RoleNames.Admin}");
        }

        var admin = await userManager.FindByEmailAsync(email) ?? await CreateAdminAsync(userManager, email);

        EnsureSucceeded(await userManager.AddToRoleAsync(admin, RoleNames.Admin), $"asignar el rol {RoleNames.Admin}");
        _logger.LogInformation("Bootstrap: el usuario {UserId} es ahora administrador.", admin.Id);
    }

    private async Task<User> CreateAdminAsync(UserManager<User> userManager, string email)
    {
        if (string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException(
                $"{BootstrapSettings.SectionName}:Admin:Password es obligatorio para crear el administrador {email}.");
        }

        var admin = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = _settings.FirstName,
            LastName = _settings.LastName,
            IsActive = true
        };

        EnsureSucceeded(await userManager.CreateAsync(admin, _settings.Password), $"crear el administrador {email}");

        return admin;
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Bootstrap: no se pudo {action}: {string.Join(" ", result.Errors.Select(error => error.Description))}");
        }
    }
}
