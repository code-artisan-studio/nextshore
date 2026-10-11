using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class OccupationListEntryConfiguration : IEntityTypeConfiguration<OccupationListEntry>
{
    public void Configure(EntityTypeBuilder<OccupationListEntry> builder)
    {
        builder.ConfigureSourcedRule();
        builder.HasIndex(o => new { o.PathwayId, o.Code }).IsUnique();
        builder.HasOne(o => o.Pathway)
            .WithMany(p => p.Occupations)
            .HasForeignKey(o => o.PathwayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
