using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbitCoder.TMTBucaramanga.Identity.Infrastructure.Persistence.Configurations;

public class AccountAuditEntryConfiguration : IEntityTypeConfiguration<AccountAuditEntry>
{
    public void Configure(EntityTypeBuilder<AccountAuditEntry> builder)
    {
        builder.Property(entry => entry.ActorUserId).HasMaxLength(256).IsRequired();
        builder.Property(entry => entry.Action).HasConversion<string>().HasMaxLength(40);
        builder.Property(entry => entry.TargetName).HasMaxLength(201).IsRequired();

        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => entry.TargetUserId);
    }
}
