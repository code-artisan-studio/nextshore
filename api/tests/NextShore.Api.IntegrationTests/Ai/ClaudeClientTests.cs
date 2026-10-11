using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Infrastructure.Ai;

namespace NextShore.Api.IntegrationTests.Ai;

public sealed class ClaudeClientTests
{
    private const string OkMessage = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,
         "usage":{"input_tokens":1,"output_tokens":1}}
        """;

    private static readonly Func<CancellationToken, Task<HttpResponseMessage>> Ok = FakeClaude.Reply(OkMessage);
    private static readonly Func<CancellationToken, Task<HttpResponseMessage>> Hang = FakeClaude.Hang;

    private static Func<CancellationToken, Task<HttpResponseMessage>> Status(HttpStatusCode status, string body) =>
        FakeClaude.Status(status, body);

    private static string Error(string type) => FakeClaude.Error(type);

    private static ClaudeClient Create(FakeClaude fake, Action<AnthropicSettings>? configure = null)
    {
        var settings = new AnthropicSettings { ApiKey = "test-key" };
        configure?.Invoke(settings);
        return new ClaudeClient(Options.Create(settings), TimeProvider.System, NullLogger<ClaudeClient>.Instance, fake);
    }

    private static Task<string> Ask(ClaudeClient claude, ClaudePurpose purpose, CancellationToken ct = default) =>
        claude.SendAsync(purpose, async (client, model, token) =>
        {
            var message = await client.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = 16,
                Messages = [new() { Role = Role.User, Content = "ping" }],
            }, token);
            return message.Content.Select(b => b.Value).OfType<TextBlock>().Single().Text;
        }, ct);

    [Theory]
    [InlineData(ClaudePurpose.ResumeParse, "claude-haiku-5-5")]
    [InlineData(ClaudePurpose.Summary, "claude-sonnet-5-5")]
    public async Task Each_purpose_uses_its_model_and_the_api_key(ClaudePurpose purpose, string model)
    {
        var fake = new FakeClaude(Ok);
        using var claude = Create(fake);

        Assert.Equal("ok", await Ask(claude, purpose));

        var request = Assert.Single(fake.Requests);
        Assert.Equal(model, JsonDocument.Parse(request.Body).RootElement.GetProperty("model").GetString());
        Assert.Equal("test-key", request.ApiKey);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "rate_limit_error")]
    [InlineData(HttpStatusCode.InternalServerError, "api_error")]
    [InlineData((HttpStatusCode)529, "overloaded_error")]
    public async Task Retries_once_then_succeeds(HttpStatusCode status, string errorType)
    {
        var fake = new FakeClaude(Status(status, Error(errorType)), Ok);
        using var claude = Create(fake);

        Assert.Equal("ok", await Ask(claude, ClaudePurpose.ResumeParse));
        Assert.Equal(2, fake.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "rate_limit_error", AiFailure.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "api_error", AiFailure.Unavailable)]
    public async Task Gives_up_after_one_retry(HttpStatusCode status, string errorType, AiFailure expected)
    {
        var fake = new FakeClaude(Status(status, Error(errorType)));
        using var claude = Create(fake);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() => Ask(claude, ClaudePurpose.Summary));

        Assert.Equal(expected, ex.Failure);
        Assert.Equal(2, fake.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "invalid_request_error")]
    [InlineData(HttpStatusCode.Unauthorized, "authentication_error")]
    public async Task Rejected_requests_are_not_retried(HttpStatusCode status, string errorType)
    {
        var fake = new FakeClaude(Status(status, Error(errorType)));
        using var claude = Create(fake);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() => Ask(claude, ClaudePurpose.ResumeParse));

        Assert.Equal(AiFailure.Rejected, ex.Failure);
        Assert.Single(fake.Requests);
    }

    [Fact]
    public async Task Whole_call_is_cut_off_at_the_purpose_timeout()
    {
        var fake = new FakeClaude(Hang);
        using var claude = Create(fake, s => s.ParseTimeout = TimeSpan.FromMilliseconds(300));

        var started = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<AiProviderException>(() => Ask(claude, ClaudePurpose.ResumeParse));

        Assert.Equal(AiFailure.Timeout, ex.Failure);
        Assert.True(started.Elapsed < TimeSpan.FromSeconds(3), $"took {started.Elapsed}");
    }

    [Fact]
    public async Task Caller_cancellation_is_not_reported_as_a_provider_failure()
    {
        var fake = new FakeClaude(Hang);
        using var claude = Create(fake);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Ask(claude, ClaudePurpose.Summary, cts.Token));
    }

    [Fact]
    public async Task Missing_api_key_fails_without_calling_claude()
    {
        var fake = new FakeClaude(Ok);
        using var claude = Create(fake, s => s.ApiKey = null);

        var ex = await Assert.ThrowsAsync<AiProviderException>(() => Ask(claude, ClaudePurpose.ResumeParse));

        Assert.Equal(AiFailure.NotConfigured, ex.Failure);
        Assert.False(claude.IsConfigured);
        Assert.Empty(fake.Requests);
    }
}
