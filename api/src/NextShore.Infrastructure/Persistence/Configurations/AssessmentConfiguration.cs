using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Profile).HasColumnType("jsonb");
        builder.Property(a => a.Results).HasColumnType("jsonb");
        builder.HasIndex(a => a.ExpiresAt);
    }
}
