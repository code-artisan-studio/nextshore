using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Application.Results;
using NextShore.Infrastructure.Ai;

namespace NextShore.Api.IntegrationTests.Ai;

public sealed class ClaudeSummaryWriterTests
{
    private static readonly EvaluationResponse Results = new(
    [
        new PathwayResultResponse("de-blue-card", "EU Blue Card", "DE", "near_eligible", 60, null,
            [new RequirementResponse(Guid.NewGuid(), "Job offer in Germany", false, "No", "Yes", true, "medium")],
            [new GapResponse(Guid.NewGuid(), "job_offer.DE", "Job offer in Germany", "No", "Yes", "medium",
                "Secure a job offer", 6, 0, "eligible")],
            ["de-blue-card"], [new SourceResponse("EU Blue Card", "https://example.gov", new DateOnly(2026, 10, 10))], 0),
    ], []);

    private static (ClaudeSummaryWriter Writer, FakeClaude Fake) Create(params Func<CancellationToken, Task<HttpResponseMessage>>[] responses)
    {
        var fake = new FakeClaude(responses);
        var claude = new ClaudeClient(Options.Create(new AnthropicSettings { ApiKey = "test-key" }), TimeProvider.System,
            NullLogger<ClaudeClient>.Instance, fake);
        return (new ClaudeSummaryWriter(claude), fake);
    }

    [Fact]
    public async Task Summary_runs_on_the_summary_model_with_server_side_fallback()
    {
        var (writer, fake) = Create(FakeClaude.Reply(FakeClaude.Message("Your best match is the EU Blue Card.")));

        var summary = await writer.WriteAsync("""{"goal":"pr"}""", Results, stricter: false, CancellationToken.None);

        Assert.Equal("Your best match is the EU Blue Card.", summary);
        var body = fake.LastBody;
        Assert.Equal("claude-sonnet-5-5", body.GetProperty("model").GetString());
        Assert.Equal("default", body.GetProperty("fallbacks").GetString());
        Assert.Contains(ClaudeSummaryWriter.FallbackBeta, fake.BetaHeaders[^1]);
        Assert.Equal("low", body.GetProperty("output_config").GetProperty("effort").GetString());
        Assert.Contains("as a chance, probability, odds, likelihood, percentage or guarantee", body.GetProperty("system").ToString());
    }

    [Fact]
    public async Task Input_is_the_profile_and_a_compact_view_of_the_results()
    {
        var (writer, fake) = Create(FakeClaude.Reply(FakeClaude.Message("Summary.")));

        await writer.WriteAsync("""{"goal":"pr"}""", Results, stricter: false, CancellationToken.None);

        var input = fake.LastBody.GetProperty("messages")[0].GetProperty("content").GetString()!;
        Assert.Contains("<profile>\n{\"goal\":\"pr\"}\n</profile>", input);
        Assert.Contains("\"pathwayName\":\"EU Blue Card\"", input);
        Assert.Contains("\"fixHint\":\"Secure a job offer\"", input);
        Assert.DoesNotContain("https://example.gov", input); // sources are not needed for the summary
        Assert.EndsWith("Write the summary.", input);
    }

    [Fact]
    public async Task Stricter_retry_restates_the_rules()
    {
        var (writer, fake) = Create(FakeClaude.Reply(FakeClaude.Message("Summary.")));

        await writer.WriteAsync("{}", Results, stricter: true, CancellationToken.None);

        Assert.Contains("do not use the words chance, probability",
            fake.LastBody.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData("", "refusal", AiFailure.Rejected)]
    [InlineData("  ", "end_turn", AiFailure.Unavailable)]
    public async Task Refused_or_empty_summary_is_a_provider_failure(string text, string stopReason, AiFailure expected)
    {
        var (writer, _) = Create(FakeClaude.Reply(FakeClaude.Message(text, stopReason)));

        var ex = await Assert.ThrowsAsync<AiProviderException>(() => writer.WriteAsync("{}", Results, false, CancellationToken.None));

        Assert.Equal(expected, ex.Failure);
    }
}
