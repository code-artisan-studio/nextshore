using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NextShore.Domain.Enums;
using NextShore.Infrastructure.Persistence;
using NextShore.Infrastructure.Seed;

namespace NextShore.Api.IntegrationTests.Database;

internal static class DatabaseHelpers
{
    public static async Task<SeedReport> SeedAsync(string connectionString)
    {
        await using var db = PostgresFixture.CreateContext(connectionString);
        return await new SeedLoader(db, NullLogger<SeedLoader>.Instance).LoadAsync(SeedPaths.Directory);
    }

    /// <summary>
    /// Stand-in for admin publish (T34): publishes the pathways and their verified rows, and every verified
    /// language mapping.
    /// </summary>
    public static async Task PublishAsync(string connectionString, params string[] slugs)
    {
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.Pathways.Where(p => slugs.Contains(p.Slug)).ExecuteUpdateAsync(set => set
            .SetProperty(p => p.Status, RuleStatus.Published)
            .SetProperty(p => p.RuleVersion, 1));
        await db.Requirements.Where(r => slugs.Contains(r.Pathway!.Slug) && r.VerifiedAt != null && r.UnverifiedFields.Count == 0)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, RuleStatus.Published));
        await db.PointsFactors.Where(f => slugs.Contains(f.Pathway!.Slug) && f.VerifiedAt != null && f.UnverifiedFields.Count == 0)
            .ExecuteUpdateAsync(set => set.SetProperty(f => f.Status, RuleStatus.Published));
        await db.OccupationLists.Where(o => slugs.Contains(o.Pathway!.Slug) && o.VerifiedAt != null && o.UnverifiedFields.Count == 0)
            .ExecuteUpdateAsync(set => set.SetProperty(o => o.Status, RuleStatus.Published));
        await db.LanguageScoreMappings.Where(m => m.VerifiedAt != null && m.UnverifiedFields.Count == 0)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.Status, RuleStatus.Published));
    }
}
