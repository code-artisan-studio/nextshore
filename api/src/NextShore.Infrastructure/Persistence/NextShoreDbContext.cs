using Microsoft.EntityFrameworkCore;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Infrastructure.Persistence.Converters;

namespace NextShore.Infrastructure.Persistence;

public sealed class NextShoreDbContext(DbContextOptions<NextShoreDbContext> options, TimeProvider timeProvider)
    : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Pathway> Pathways => Set<Pathway>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<PointsFactor> PointsFactors => Set<PointsFactor>();
    public DbSet<OccupationListEntry> OccupationLists => Set<OccupationListEntry>();
    public DbSet<LanguageScoreMapping> LanguageScoreMappings => Set<LanguageScoreMapping>();
    public DbSet<RuleAuditLogEntry> RuleAuditLog => Set<RuleAuditLogEntry>();
    public DbSet<Assessment> Assessments => Set<Assessment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NextShoreDbContext).Assembly);

        // Table names follow DATA-MODEL.md rather than entity class names.
        modelBuilder.Entity<OccupationListEntry>().ToTable("occupation_lists");
        modelBuilder.Entity<RuleAuditLogEntry>().ToTable("rule_audit_log");
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<RuleStatus>().HaveConversion<SnakeCaseEnumConverter<RuleStatus>>();
        configurationBuilder.Properties<PathwayType>().HaveConversion<SnakeCaseEnumConverter<PathwayType>>();
        configurationBuilder.Properties<RequirementOperator>().HaveConversion<SnakeCaseEnumConverter<RequirementOperator>>();
        configurationBuilder.Properties<Fixability>().HaveConversion<SnakeCaseEnumConverter<Fixability>>();
        configurationBuilder.Properties<PointsVariant>().HaveConversion<SnakeCaseEnumConverter<PointsVariant>>();
        configurationBuilder.Properties<AuditAction>().HaveConversion<SnakeCaseEnumConverter<AuditAction>>();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampRuleTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRuleTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void StampRuleTimestamps()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<RuleEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
