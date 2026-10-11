using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NextShore.Api.IntegrationTests.Ai;

/// <summary>Stands in for the Claude API: answers with queued responses (the last repeats) and records requests.</summary>
internal sealed class FakeClaude(params Func<CancellationToken, Task<HttpResponseMessage>>[] responses) : HttpMessageHandler
{
    public List<(string Body, string? ApiKey)> Requests { get; } = [];

    public List<string> BetaHeaders { get; } = [];

    public JsonElement LastBody => JsonDocument.Parse(Requests[^1].Body).RootElement;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((await request.Content!.ReadAsStringAsync(cancellationToken),
            request.Headers.TryGetValues("x-api-key", out var key) ? key.Single() : null));
        BetaHeaders.Add(request.Headers.TryGetValues("anthropic-beta", out var betas) ? string.Join(",", betas) : string.Empty);
        return await responses[Math.Min(Requests.Count, responses.Length) - 1](cancellationToken);
    }

    public static Func<CancellationToken, Task<HttpResponseMessage>> Status(HttpStatusCode status, string body) => _ =>
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        response.Headers.Add("retry-after-ms", "10");
        return Task.FromResult(response);
    };

    public static Func<CancellationToken, Task<HttpResponseMessage>> Reply(string messageJson) => Status(HttpStatusCode.OK, messageJson);

    /// <summary>A Messages API response with one text block and the given stop reason.</summary>
    public static string Message(string text, string stopReason = "end_turn") => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "claude-haiku-5-5",
        content = new[] { new { type = "text", text } },
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 10, output_tokens = 10 },
    });

    public static string Error(string type) =>
        JsonSerializer.Serialize(new { type = "error", error = new { type, message = "test" } });

    public static readonly Func<CancellationToken, Task<HttpResponseMessage>> Hang = async ct =>
    {
        await Task.Delay(Timeout.Infinite, ct);
        throw new UnreachableException();
    };
}
