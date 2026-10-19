using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>SHA-256 en hexadecimal.</summary>
    private const int TokenHashLength = 64;

    /// <summary>
    ///     Configura el mapeo de la entidad &lt;see cref="RefreshToken"/&gt;.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Dos peticiones simultáneas con el mismo token no pueden rotarlo las dos: la segunda falla con
    ///     DbUpdateConcurrencyException al encontrarlo ya revocado.
    /// </para>
    /// <para>
    ///     Mismo filtro que User (borrado lógico): los refresh tokens de un usuario eliminado no existen para la
    ///     aplicación, así que no se pueden usar.
    /// </para>
    /// </remarks>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasMaxLength(TokenHashLength)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique();

        builder.HasIndex(refreshToken => refreshToken.FamilyId);

        builder.Property(refreshToken => refreshToken.Audience)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAt)
            .IsConcurrencyToken();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(refreshToken => !refreshToken.User.IsDeleted);
    }
}
