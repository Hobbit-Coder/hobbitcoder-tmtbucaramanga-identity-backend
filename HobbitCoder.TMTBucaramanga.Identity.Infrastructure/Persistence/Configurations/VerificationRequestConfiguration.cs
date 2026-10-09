using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class VerificationRequestConfiguration : IEntityTypeConfiguration<VerificationRequest>
{
    /// <summary>SHA-256 en hexadecimal.</summary>
    private const int HashLength = 64;

    public void Configure(EntityTypeBuilder<VerificationRequest> builder)
    {
        builder.Property(request => request.Purpose).HasConversion<string>().HasMaxLength(20);
        builder.Property(request => request.Channel).HasConversion<string>().HasMaxLength(20);

        builder.Property(request => request.CodeHash)
            .HasMaxLength(HashLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(request => request.ConfirmationTokenHash)
            .HasMaxLength(HashLength)
            .IsFixedLength();

        builder.HasIndex(request => request.ConfirmationTokenHash)
            .IsUnique();

        // Dos confirmaciones o dos usos simultáneos del mismo token no pueden tener éxito los dos.
        builder.Property(request => request.ConfirmedAt).IsConcurrencyToken();
        builder.Property(request => request.CompletedAt).IsConcurrencyToken();
        builder.Property(request => request.FailedAttempts).IsConcurrencyToken();

        builder.HasOne(request => request.User)
            .WithMany(user => user.VerificationRequests)
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(request => !request.User.IsDeleted);
    }
}
