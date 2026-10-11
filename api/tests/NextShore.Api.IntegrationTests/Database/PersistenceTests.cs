using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Infrastructure.Assessments;
using NextShore.Infrastructure.Persistence;
using NextShore.Infrastructure.Rules;
using NextShore.Infrastructure.Seed;

namespace NextShore.Api.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Migrations_build_the_schema_and_match_the_model()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);

        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "The model has changes without a migration.");
        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' ORDER BY 1")
            .ToListAsync();
        Assert.Equal(
            ["__EFMigrationsHistory", "assessments", "countries", "language_score_mappings", "occupation_lists", "pathways",
             "points_factors", "requirements", "rule_audit_log"],
            tables);
    }

    [Fact]
    public async Task Seed_loader_writes_every_seed_file_and_reloads_in_place()
    {
        var connectionString = await postgres.CreateDatabaseAsync();

        var first = await DatabaseHelpers.SeedAsync(connectionString);
        var counts = await CountsAsync(connectionString);
        var ids = await IdsAsync(connectionString);
        var second = await DatabaseHelpers.SeedAsync(connectionString);

        Assert.All(first.Pathways, p => Assert.Equal(SeedOutcome.Created, p.Outcome));
        Assert.All(second.Pathways, p => Assert.Equal(SeedOutcome.Updated, p.Outcome));
        Assert.Equal((3, 10), (counts.Countries, counts.Pathways));
        Assert.Equal(first.Pathways.Sum(p => p.Requirements), counts.Requirements);
        Assert.Equal(first.Pathways.Sum(p => p.PointsFactors), counts.PointsFactors);
        Assert.Equal(first.LanguageMappingsCreated, counts.Mappings);
        Assert.Equal(counts, await CountsAsync(connectionString)); // nothing duplicated
        Assert.Equal(ids, await IdsAsync(connectionString));       // pathways updated, not replaced
    }

    [Fact]
    public async Task Seed_loader_leaves_published_pathways_alone()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await DatabaseHelpers.SeedAsync(connectionString);
        await DatabaseHelpers.PublishAsync(connectionString, "ca-pgwp");
        await using (var db = PostgresFixture.CreateContext(connectionString))
        {
            await db.Pathways.Where(p => p.Slug == "ca-pgwp").ExecuteUpdateAsync(set => set.SetProperty(p => p.Name, "Edited by admin"));
        }

        var report = await DatabaseHelpers.SeedAsync(connectionString);

        Assert.Equal(SeedOutcome.SkippedPublished, report.Pathways.Single(p => p.Slug == "ca-pgwp").Outcome);
        await using var check = PostgresFixture.CreateContext(connectionString);
        Assert.Equal("Edited by admin", await check.Pathways.Where(p => p.Slug == "ca-pgwp").Select(p => p.Name).SingleAsync());
    }

    [Fact]
    public async Task Rule_snapshot_reads_only_published_rows()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await DatabaseHelpers.SeedAsync(connectionString);
        await DatabaseHelpers.PublishAsync(connectionString, "ca-pgwp", "de-post-study");
        await using var services = new ServiceCollection()
            .AddDbContext<NextShoreDbContext>(o => o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention())
            .AddSingleton(TimeProvider.System)
            .BuildServiceProvider();

        var snapshot = await new DbRuleSnapshotSource(services.GetRequiredService<IServiceScopeFactory>()).LoadAsync(CancellationToken.None);

        Assert.Equal(["ca-pgwp", "de-post-study"], snapshot.Pathways.Select(p => p.Slug));
        Assert.Equal(1, snapshot.Version);
        Assert.Equal(["AU", "CA", "DE"], snapshot.Countries.Select(c => c.Code));
        var pgwp = snapshot.FindPathway("ca-pgwp")!;
        Assert.Null(pgwp.NextPathwaySlug); // Express Entry is still a draft
        Assert.Equal(36, pgwp.TypicalDurationMonths);
        Assert.Equal("English CLB 7 in all four skills (university programs; CLB 5 for college programs)", Assert.Single(pgwp.Requirements).Label);
        Assert.Equal(56, snapshot.LanguageMappings.Count); // the verified ones
    }

    [Fact]
    public async Task Assessment_store_round_trips_caches_the_first_summary_and_deletes_expired()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        Assessment Row(DateTimeOffset expiresAt) => new()
        {
            Id = Guid.CreateVersion7(), Profile = """{"goal":"pr"}""", Results = """{"results":[],"chains":[]}""",
            RuleVersion = 1, CreatedAt = Now, ExpiresAt = expiresAt,
        };
        var live = Row(Now.AddDays(30));
        var expired = Row(Now.AddSeconds(-1));
        await using (var db = PostgresFixture.CreateContext(connectionString))
        {
            var store = new AssessmentStore(db);
            await store.AddAsync(live, CancellationToken.None);
            await store.AddAsync(expired, CancellationToken.None);
        }

        await using (var db = PostgresFixture.CreateContext(connectionString))
        {
            var store = new AssessmentStore(db);
            var found = (await store.FindAsync(live.Id, CancellationToken.None))!;
            Assert.Equal("pr", System.Text.Json.JsonDocument.Parse(found.Profile).RootElement.GetProperty("goal").GetString());

            var first = await store.SaveSummaryAsync(live.Id, "First", Now, CancellationToken.None);
            var second = await store.SaveSummaryAsync(live.Id, "Second", Now.AddMinutes(1), CancellationToken.None);
            Assert.Equal(("First", Now), first);
            Assert.Equal(first, second);

            Assert.Equal(1, await store.DeleteExpiredAsync(Now, CancellationToken.None));
            Assert.Null(await store.FindAsync(expired.Id, CancellationToken.None));
            Assert.NotNull(await store.FindAsync(live.Id, CancellationToken.None));
        }
    }

    private static async Task<(int Countries, int Pathways, int Requirements, int PointsFactors, int Mappings)> CountsAsync(string cs)
    {
        await using var db = PostgresFixture.CreateContext(cs);
        return (await db.Countries.CountAsync(), await db.Pathways.CountAsync(), await db.Requirements.CountAsync(),
            await db.PointsFactors.CountAsync(), await db.LanguageScoreMappings.CountAsync());
    }

    private static async Task<List<Guid>> IdsAsync(string cs)
    {
        await using var db = PostgresFixture.CreateContext(cs);
        return await db.Pathways.OrderBy(p => p.Slug).Select(p => p.Id).ToListAsync();
    }
}
