using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextShore.Domain.Entities;

namespace NextShore.Infrastructure.Persistence.Configurations;

internal static class RuleEntityConfiguration
{
    public static EntityTypeBuilder<T> ConfigureRule<T>(this EntityTypeBuilder<T> builder)
        where T : RuleEntity
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Status);
        return builder;
    }

    public static EntityTypeBuilder<T> ConfigureSourcedRule<T>(this EntityTypeBuilder<T> builder)
        where T : SourcedRuleEntity
    {
        builder.ConfigureRule();
        builder.Ignore(e => e.IsPublishable);
        builder.Property(e => e.UnverifiedFields).HasColumnType("text[]");
        return builder;
    }
}
