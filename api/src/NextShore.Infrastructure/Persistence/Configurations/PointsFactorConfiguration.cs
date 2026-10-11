using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class PointsFactorConfiguration : IEntityTypeConfiguration<PointsFactor>
{
    public void Configure(EntityTypeBuilder<PointsFactor> builder)
    {
        builder.ConfigureSourcedRule();
        builder.HasOne(f => f.Pathway)
            .WithMany(p => p.PointsFactors)
            .HasForeignKey(f => f.PathwayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
