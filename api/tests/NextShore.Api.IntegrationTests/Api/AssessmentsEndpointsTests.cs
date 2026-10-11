using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NextShore.Application.Abstractions;
using NextShore.Application.Assessments;
using NextShore.Domain.Entities;
using NextShore.Infrastructure.Assessments;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class AssessmentsEndpointsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Post_stores_the_assessment_and_returns_201_with_its_location()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/assessments", Personas.MidDeveloper);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AssessmentResponse>())!;
        Assert.Equal($"/api/v1/assessments/{body.Id}", response.Headers.Location!.ToString());
        Assert.Equal(1, body.RuleVersion);
        Assert.Equal(Now.AddDays(30), body.ExpiresAt);
        Assert.Equal(["au-189", "de-blue-card", "ca-express-entry", "de-opportunity-card"], body.Results.Select(r => r.PathwaySlug));

        var row = Assert.Single(factory.Assessments.Rows.Values);
        Assert.Equal((body.Id, Now, Now.AddDays(30), 1), (row.Id, row.CreatedAt, row.ExpiresAt, row.RuleVersion));
        Assert.Equal("pr", JsonDocument.Parse(row.Profile).RootElement.GetProperty("goal").GetString());
        Assert.Equal(4, JsonDocument.Parse(row.Results).RootElement.GetProperty("results").GetArrayLength());
    }

    [Fact]
    public async Task Get_returns_exactly_what_post_returned()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/v1/assessments", Personas.Student)).Content.ReadAsStringAsync();
        var id = JsonDocument.Parse(created).RootElement.GetProperty("id").GetString();

        var fetched = await client.GetStringAsync($"/api/v1/assessments/{id}");

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Get_serves_the_stored_results_not_a_re_evaluation()
    {
        using var factory = new ApiFactory();
        var stored = new Assessment
        {
            Id = Guid.NewGuid(),
            Profile = "{}",
            Results = """{"results":[{"pathwaySlug":"xx-old","grade":"eligible","readinessScore":100}],"chains":[]}""",
            RuleVersion = 3,
            CreatedAt = Now.AddDays(-2),
            ExpiresAt = Now.AddDays(28),
        };
        factory.Assessments.Rows[stored.Id] = stored;

        var body = await factory.CreateClient().GetFromJsonAsync<AssessmentResponse>($"/api/v1/assessments/{stored.Id}");

        Assert.Equal(("xx-old", 3), (Assert.Single(body!.Results).PathwaySlug, body.RuleVersion));
    }

    [Fact]
    public async Task Unknown_or_expired_assessment_is_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/v1/assessments", Personas.MidDeveloper)).Content
            .ReadFromJsonAsync<AssessmentResponse>();

        factory.Clock.Advance(TimeSpan.FromDays(30));
        var expired = await client.GetAsync($"/api/v1/assessments/{created!.Id}");
        var unknown = await client.GetAsync($"/api/v1/assessments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        Assert.Equal("application/problem+json", expired.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Invalid_profile_is_400_and_nothing_is_stored()
    {
        using var factory = new ApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/assessments", Personas.MidDeveloper with { Goal = null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Assessments.Rows);
    }

    [Fact]
    public async Task Cleanup_deletes_expired_assessments_at_startup_and_every_interval()
    {
        var clock = new FakeTimeProvider(Now);
        var store = new InMemoryAssessmentStore();
        foreach (var row in new[] { Row(expiresAt: Now.AddDays(-1)), Row(expiresAt: Now.AddHours(12)) })
        {
            store.Rows[row.Id] = row;
        }
        var services = new ServiceCollection().AddSingleton<IAssessmentStore>(store).BuildServiceProvider();
        using var service = new AssessmentCleanupService(services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AssessmentSettings()), clock, NullLogger<AssessmentCleanupService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await WaitUntil(() => store.CleanupCalls.Count == 1);
        Assert.Single(store.Rows); // the already-expired one is gone

        clock.Advance(TimeSpan.FromDays(1));
        await WaitUntil(() => store.CleanupCalls.Count == 2);
        Assert.Empty(store.Rows);
        Assert.Equal([Now, Now.AddDays(1)], store.CleanupCalls);

        await service.StopAsync(CancellationToken.None);
    }

    private static Assessment Row(DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), Profile = "{}", Results = "{}", CreatedAt = Now.AddDays(-30), ExpiresAt = expiresAt,
    };

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20);
        }

        Assert.True(condition());
    }
}
