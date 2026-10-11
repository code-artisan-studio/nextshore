using Microsoft.EntityFrameworkCore;
using Npgsql;
using NextShore.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NextShore.Api.IntegrationTests.Database;

/// <summary>
/// One Postgres container (same major version as Neon) per test run. Each test gets its own freshly migrated
/// database, so tests never see each other's rows. Needs Docker.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates an empty database and returns its connection string.</summary>
    public async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
    }

    /// <summary>Creates a database and applies every migration.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await CreateEmptyDatabaseAsync();
        await using var db = CreateContext(connectionString);
        await db.Database.MigrateAsync();
        return connectionString;
    }

    public static NextShoreDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<NextShoreDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            TimeProvider.System);
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
