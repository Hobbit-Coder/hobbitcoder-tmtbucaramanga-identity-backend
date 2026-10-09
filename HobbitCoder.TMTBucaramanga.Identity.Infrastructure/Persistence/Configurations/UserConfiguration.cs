using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const int NameMaxLength = 100;
    public const int PhoneNumberMaxLength = 30;
    public const int InstagramMaxLength = 60;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.FirstName)
            .HasMaxLength(NameMaxLength)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(NameMaxLength)
            .IsRequired();

        builder.Property(user => user.Gender)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.Instagram)
            .HasMaxLength(InstagramMaxLength);

        // Teléfono normalizado (+57...). Es un identificador de acceso: único entre las cuentas, incluidas las
        // eliminadas lógicamente, igual que el usuario y el correo.
        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(PhoneNumberMaxLength);

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique();

        // El correo es opcional (§5), pero si se registra identifica una sola cuenta.
        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();

        builder.HasIndex(user => user.Role);

        builder.Ignore(user => user.FullName);
    }
}
