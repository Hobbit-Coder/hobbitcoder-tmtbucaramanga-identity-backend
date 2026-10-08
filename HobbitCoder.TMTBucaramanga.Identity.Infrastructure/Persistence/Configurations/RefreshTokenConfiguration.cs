using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>SHA-256 en hexadecimal.</summary>
    private const int TokenHashLength = 64;

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

        // Dos peticiones simultáneas con el mismo token no pueden rotarlo las dos: la segunda falla
        // con DbUpdateConcurrencyException al encontrarlo ya revocado.
        builder.Property(refreshToken => refreshToken.RevokedAt)
            .IsConcurrencyToken();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mismo filtro que User (borrado lógico): los refresh tokens de un usuario eliminado no existen
        // para la aplicación, así que no se pueden usar.
        builder.HasQueryFilter(refreshToken => !refreshToken.User.IsDeleted);
    }
}
