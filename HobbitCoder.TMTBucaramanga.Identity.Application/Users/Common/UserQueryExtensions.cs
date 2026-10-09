using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public static class UserQueryExtensions
{
    /// <summary>
    /// Busca la cuenta de un identificador de acceso (§6): correo si contiene <c>@</c>, teléfono si solo
    /// tiene dígitos y separadores, y nombre de usuario en otro caso. Un correo no registrado no identifica
    /// ninguna cuenta.
    /// </summary>
    public static Task<User?> FindByLoginIdentifierAsync(
        this IApplicationDbContext dbContext, ILookupNormalizer normalizer, string identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizer);
        var trimmed = identifier.Trim();

        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            var normalizedEmail = normalizer.NormalizeEmail(trimmed);
            return dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        if (PhoneNumbers.Normalize(trimmed) is { } phoneNumber)
        {
            return dbContext.Users.FirstOrDefaultAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);
        }

        var normalizedUserName = normalizer.NormalizeName(trimmed);
        return dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    /// <summary>
    /// Comprueba que el nombre de usuario, el teléfono y el correo (si hay) no los use otra cuenta, incluidas
    /// las eliminadas lógicamente: los índices únicos de la base también las cuentan.
    /// </summary>
    /// <exception cref="ConflictException">Alguno ya está registrado.</exception>
    public static async Task EnsureIdentifiersAvailableAsync(
        this IApplicationDbContext dbContext,
        ILookupNormalizer normalizer,
        string userName,
        string phoneNumber,
        string? email,
        Guid? exceptUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizer);

        var users = dbContext.Users.IgnoreQueryFilters().Where(user => user.Id != exceptUserId);
        var normalizedUserName = normalizer.NormalizeName(userName);

        if (await users.AnyAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken))
        {
            throw new ConflictException($"The user name '{userName}' is already registered.");
        }

        if (await users.AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken))
        {
            throw new ConflictException("The phone number is already registered.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = normalizer.NormalizeEmail(email);

            if (await users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                throw new ConflictException($"The email '{email}' is already registered.");
            }
        }
    }

    /// <summary>Correo vacío como <c>null</c>: el correo es opcional.</summary>
    public static string? EmptyAsNull(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
