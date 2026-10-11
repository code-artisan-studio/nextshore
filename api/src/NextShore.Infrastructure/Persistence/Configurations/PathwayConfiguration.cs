using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Infrastructure.Persistence.Converters;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class PathwayConfiguration : IEntityTypeConfiguration<Pathway>
{
    public void Configure(EntityTypeBuilder<Pathway> builder)
    {
        builder.ConfigureSourcedRule();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.PrimitiveCollection(p => p.Audience)
            .HasColumnType("text[]")
            .ElementType()
            .HasConversion<SnakeCaseEnumConverter<Audience>>();

        builder.HasOne(p => p.Country)
            .WithMany(c => c.Pathways)
            .HasForeignKey(p => p.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.NextPathway)
            .WithMany()
            .HasForeignKey(p => p.NextPathwayId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
