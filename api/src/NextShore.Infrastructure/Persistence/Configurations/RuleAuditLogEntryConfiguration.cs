using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class RuleAuditLogEntryConfiguration : IEntityTypeConfiguration<RuleAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<RuleAuditLogEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OldValue).HasColumnType("jsonb");
        builder.Property(e => e.NewValue).HasColumnType("jsonb");
        builder.HasIndex(e => new { e.Entity, e.EntityId });
        builder.HasIndex(e => e.At);
    }
}
