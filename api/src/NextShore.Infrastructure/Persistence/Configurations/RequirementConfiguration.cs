using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ConfigureSourcedRule();
        builder.Property(r => r.Value).HasColumnType("jsonb");
        builder.HasOne(r => r.Pathway)
            .WithMany(p => p.Requirements)
            .HasForeignKey(r => r.PathwayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
