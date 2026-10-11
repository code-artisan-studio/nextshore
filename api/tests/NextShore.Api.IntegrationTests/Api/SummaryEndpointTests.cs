using System.Net;
using System.Net.Http.Json;
using NextShore.Application.Abstractions;
using NextShore.Application.Assessments;
using NextShore.Application.Results;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class SummaryEndpointTests
{
    private sealed class FakeWriter(params Func<string>[] drafts) : ISummaryWriter
    {
        public List<bool> Calls { get; } = [];

        public Task<string> WriteAsync(string profileJson, EvaluationResponse results, bool stricter, CancellationToken cancellationToken)
        {
            Calls.Add(stricter);
            return Task.FromResult(drafts[Math.Min(Calls.Count, drafts.Length) - 1]());
        }
    }

    private static async Task<(ApiFactory Factory, HttpClient Client, Guid Id)> CreateAssessment(FakeWriter writer)
    {
        var factory = new ApiFactory { SummaryWriter = writer };
        var client = factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/v1/assessments", Personas.MidDeveloper)).Content
            .ReadFromJsonAsync<AssessmentResponse>();
        return (factory, client, created!.Id);
    }

    [Fact]
    public async Task Summary_is_generated_once_then_served_from_the_assessment()
    {
        var writer = new FakeWriter(() => "Your best matches are the 189 visa and the EU Blue Card.");
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        var first = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{id}/summary");
        factory.Clock.Advance(TimeSpan.FromHours(1));
        var second = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{id}/summary");

        Assert.Equal("Your best matches are the 189 visa and the EU Blue Card.", first!.Summary);
        Assert.Equal(first, second);
        Assert.Equal(factory.Clock.GetUtcNow().AddHours(-1), first.GeneratedAt);
        Assert.Equal([false], writer.Calls);
        Assert.Equal(first.Summary, factory.Assessments.Rows[id].Summary);
    }

    [Fact]
    public async Task A_draft_with_odds_is_rewritten_once_with_stricter_rules()
    {
        var writer = new FakeWriter(() => "You have a strong chance with the 189 visa.", () => "The 189 visa is your best match.");
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        var summary = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{id}/summary");

        Assert.Equal("The 189 visa is your best match.", summary!.Summary);
        Assert.Equal([false, true], writer.Calls);
    }

    [Fact]
    public async Task Two_bad_drafts_are_a_502_and_nothing_is_cached()
    {
        var writer = new FakeWriter(() => "Approval is guaranteed.");
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        var response = await client.GetAsync($"/api/v1/assessments/{id}/summary");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(2, writer.Calls.Count);
        Assert.Null(factory.Assessments.Rows[id].Summary);
    }

    [Fact]
    public async Task Provider_failure_is_a_502()
    {
        var writer = new FakeWriter(() => throw new AiProviderException(AiFailure.Timeout, "test"));
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        Assert.Equal(HttpStatusCode.BadGateway, (await client.GetAsync($"/api/v1/assessments/{id}/summary")).StatusCode);
    }

    [Fact]
    public async Task Long_summary_is_trimmed_to_200_words()
    {
        var sentence = string.Join(' ', Enumerable.Repeat("word", 59)) + " end."; // 60 words
        var writer = new FakeWriter(() => string.Join(' ', Enumerable.Repeat(sentence, 4)));
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        var summary = await client.GetFromJsonAsync<SummaryResponse>($"/api/v1/assessments/{id}/summary");

        Assert.Equal(180, summary!.Summary.Split(' ').Length);
    }

    [Fact]
    public async Task Unknown_or_expired_assessment_is_404_without_calling_the_ai()
    {
        var writer = new FakeWriter(() => "Summary.");
        var (factory, client, id) = await CreateAssessment(writer);
        using var _ = factory;

        var unknown = await client.GetAsync($"/api/v1/assessments/{Guid.NewGuid()}/summary");
        factory.Clock.Advance(TimeSpan.FromDays(30));
        var expired = await client.GetAsync($"/api/v1/assessments/{id}/summary");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, expired.StatusCode);
        Assert.Empty(writer.Calls);
    }
}
