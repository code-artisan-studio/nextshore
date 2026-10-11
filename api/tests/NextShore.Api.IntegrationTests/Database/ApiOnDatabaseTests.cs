using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NextShore.Api.Endpoints;
using NextShore.Api.IntegrationTests.Api;
using NextShore.Application.Abstractions;
using NextShore.Application.Assessments;
using NextShore.Application.Results;

namespace NextShore.Api.IntegrationTests.Database;

/// <summary>The API end to end on a real Postgres, with only Claude replaced.</summary>
[Collection(PostgresCollection.Name)]
public sealed class ApiOnDatabaseTests(PostgresFixture postgres)
{
    private sealed class FakeSummaryWriter : ISummaryWriter
    {
        public int Calls { get; private set; }

        public Task<string> WriteAsync(string profileJson, EvaluationResponse results, bool stricter, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult($"Your best match is {results.Results[0].PathwayName}.");
        }
    }

    [Fact]
    public async Task Health_checks_the_real_database()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        using var factory = new ApiFactory { DatabaseConnectionString = connectionString };

        var health = await factory.CreateClient().GetFromJsonAsync<HealthResponse>("/api/v1/health");

        Assert.Equal("ok", health!.Status);
    }

    [Fact]
    public async Task Published_rules_drive_assessments_and_summaries_are_stored_in_postgres()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await DatabaseHelpers.SeedAsync(connectionString);
        await DatabaseHelpers.PublishAsync(connectionString, "ca-pgwp", "de-post-study");
        var writer = new FakeSummaryWriter();
        using var factory = new ApiFactory { DatabaseConnectionString = connectionString, SummaryWriter = writer };
        var client = factory.CreateClient();

        var countries = await client.GetFromJsonAsync<List<Application.Countries.CountryResponse>>("/api/v1/countries");
        var created = await client.PostAsJsonAsync("/api/v1/assessments", Personas.Student);
        var assessment = (await created.Content.ReadFromJsonAsync<AssessmentResponse>())!;
        var fetched = await client.GetFromJsonAsync<AssessmentResponse>($"/api/v1/assessments/{assessment.Id}");
        var summary = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{assessment.Id}/summary");
        var again = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{assessment.Id}/summary");

        Assert.Equal(["ca-pgwp", "de-post-study"], countries!.SelectMany(c => c.Pathways).Select(p => p.Slug).Order());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(1, assessment.RuleVersion);
        Assert.Equal(
            [("ca-pgwp", "eligible"), ("de-post-study", "near_eligible")],
            assessment.Results.Select(r => (r.PathwaySlug, r.Grade)));
        Assert.Equal(assessment.Results.Select(r => r.ReadinessScore), fetched!.Results.Select(r => r.ReadinessScore));
        Assert.Equal(summary, again);
        Assert.Equal(1, writer.Calls);

        await using var db = PostgresFixture.CreateContext(connectionString);
        var row = await db.Assessments.SingleAsync();
        Assert.Equal((assessment.Id, summary!.Summary, 1), (row.Id, row.Summary, row.RuleVersion));
        Assert.Equal("study", System.Text.Json.JsonDocument.Parse(row.Profile).RootElement.GetProperty("goal").GetString());
    }
}
