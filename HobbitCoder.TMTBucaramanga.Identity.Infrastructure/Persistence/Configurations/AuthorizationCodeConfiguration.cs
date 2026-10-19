using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class AuthorizationCodeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    /// <summary>SHA-256 en hexadecimal.</summary>
    private const int CodeHashLength = 64;

    /// <summary>Base64url de un SHA-256.</summary>
    private const int CodeChallengeLength = 43;

    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="AuthorizationCode"/&gt;.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Dos canjes simultáneos del mismo código no pueden tener éxito los dos: el segundo falla con
    ///     DbUpdateConcurrencyException al encontrarlo ya usado.
    /// </para>
    /// <para>
    ///     Mismo filtro que User (borrado lógico): los códigos de un usuario eliminado no se pueden canjear.
    /// </para>
    /// </remarks>
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.Property(authorizationCode => authorizationCode.CodeHash)
            .HasMaxLength(CodeHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(authorizationCode => authorizationCode.CodeHash)
            .IsUnique();

        builder.Property(authorizationCode => authorizationCode.CodeChallenge)
            .HasMaxLength(CodeChallengeLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(authorizationCode => authorizationCode.ClientId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(authorizationCode => authorizationCode.RedirectUri)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(authorizationCode => authorizationCode.ConsumedAt)
            .IsConcurrencyToken();

        builder.HasOne(authorizationCode => authorizationCode.User)
            .WithMany(user => user.AuthorizationCodes)
            .HasForeignKey(authorizationCode => authorizationCode.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(authorizationCode => !authorizationCode.User.IsDeleted);
    }
}
