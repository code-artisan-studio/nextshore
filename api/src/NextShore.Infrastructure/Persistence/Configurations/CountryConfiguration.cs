using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ConfigureRule();
        builder.Property(c => c.Code).HasMaxLength(2).IsFixedLength();
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
