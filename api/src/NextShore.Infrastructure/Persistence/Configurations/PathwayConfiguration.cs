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
        // One live row per slug, plus at most one admin draft copy of it.
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("replaces_id IS NULL");
        builder.HasIndex(p => p.ReplacesId).IsUnique().HasFilter("replaces_id IS NOT NULL");

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
