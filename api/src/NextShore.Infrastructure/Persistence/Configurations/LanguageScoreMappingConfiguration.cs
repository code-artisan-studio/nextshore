using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal sealed class LanguageScoreMappingConfiguration : IEntityTypeConfiguration<LanguageScoreMapping>
{
    public void Configure(EntityTypeBuilder<LanguageScoreMapping> builder)
    {
        builder.ConfigureSourcedRule();
        builder.HasIndex(m => new { m.Test, m.Skill, m.TargetScale });
    }
}
