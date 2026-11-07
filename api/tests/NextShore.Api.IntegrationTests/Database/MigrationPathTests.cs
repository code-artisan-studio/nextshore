using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace NextShore.Api.IntegrationTests.Database;

/// <summary>
/// Production and development were migrated to AdminDrafts (an earlier M5 design) before AdminDraftCopies existed.
/// Both upgrade paths must end at the same schema: empty → all migrations, and AdminDrafts → AdminDraftCopies.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class MigrationPathTests(PostgresFixture postgres)
{
    private const string AdminDrafts = "20261020025218_AdminDrafts";

    [Fact]
    public async Task Empty_database_migrates_to_the_current_model()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var db = PostgresFixture.CreateContext(connectionString);

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains(AdminDrafts, await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Database_at_AdminDrafts_upgrades_to_the_same_schema_as_a_fresh_one_and_keeps_its_rows()
    {
        var fresh = await postgres.CreateDatabaseAsync();
        var upgraded = await postgres.CreateEmptyDatabaseAsync();
        await MigrateToAsync(upgraded, AdminDrafts);
        var seeded = await DatabaseHelpers.SeedAsync(upgraded); // the seed loader works on the AdminDrafts schema
        await DatabaseHelpers.PublishAsync(upgraded, "ca-pgwp");
        var before = await CountsAsync(upgraded);

        await MigrateToAsync(upgraded, null);

        Assert.Equal(await SchemaAsync(fresh), await SchemaAsync(upgraded));
        Assert.Equal(before, await CountsAsync(upgraded));
        Assert.Equal(10, seeded.Pathways.Count);
        await using var db = PostgresFixture.CreateContext(upgraded);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Theory]
    [InlineData("UPDATE requirements SET delete_on_publish = true WHERE id = (SELECT id FROM requirements LIMIT 1)", "requirements")]
    [InlineData("UPDATE language_score_mappings SET replaces_id = id WHERE id = (SELECT id FROM language_score_mappings LIMIT 1)", "language_score_mappings")]
    [InlineData("UPDATE occupation_lists SET status = 'archived' WHERE id = (SELECT id FROM occupation_lists LIMIT 1)", "occupation_lists")]
    public async Task Leftover_AdminDrafts_data_aborts_the_upgrade_without_changing_anything(string leftover, string table)
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        await MigrateToAsync(connectionString, AdminDrafts);
        await DatabaseHelpers.SeedAsync(connectionString);
        await ExecuteAsync(connectionString, leftover);

        var error = await Assert.ThrowsAsync<PostgresException>(() => MigrateToAsync(connectionString, null));

        Assert.Contains(table, error.MessageText);
        await using var db = PostgresFixture.CreateContext(connectionString);
        Assert.Equal(AdminDrafts, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Contains(await SchemaAsync(connectionString), c => c.StartsWith("requirements.delete_on_publish ")); // rolled back
    }

    private static async Task MigrateToAsync(string connectionString, string? target)
    {
        await using var db = PostgresFixture.CreateContext(connectionString);
        await db.GetService<IMigrator>().MigrateAsync(target);
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }

    /// <summary>Columns, indexes and constraints of the public schema, normalised for comparison.</summary>
    private static async Task<List<string>> SchemaAsync(string connectionString) => await QueryAsync(connectionString, """
        SELECT table_name || '.' || column_name || ' ' || data_type || ' null=' || is_nullable || ' default=' || coalesce(column_default, '')
          FROM information_schema.columns WHERE table_schema = 'public'
        UNION ALL
        SELECT 'index ' || indexname || ': ' || indexdef FROM pg_indexes WHERE schemaname = 'public'
        UNION ALL
        SELECT 'constraint ' || conrelid::regclass || ' ' || conname || ': ' || pg_get_constraintdef(oid)
          FROM pg_constraint WHERE connamespace = 'public'::regnamespace
        ORDER BY 1
        """);

    private static async Task<List<string>> CountsAsync(string connectionString) => await QueryAsync(connectionString, """
        SELECT 'pathways ' || count(*) || ' published ' || count(*) FILTER (WHERE status = 'published') FROM pathways
        UNION ALL SELECT 'requirements ' || count(*) FROM requirements
        UNION ALL SELECT 'points_factors ' || count(*) FROM points_factors
        UNION ALL SELECT 'occupation_lists ' || count(*) FROM occupation_lists
        UNION ALL SELECT 'language_score_mappings ' || count(*) FROM language_score_mappings
        UNION ALL SELECT 'countries ' || count(*) FROM countries
        """);
}
