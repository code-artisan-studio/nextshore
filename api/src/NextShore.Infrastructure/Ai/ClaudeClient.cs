using System.Diagnostics;
using Anthropic;
using Anthropic.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;

namespace NextShore.Infrastructure.Ai;

public enum ClaudePurpose
{
    ResumeParse,
    Summary,
}

/// <summary>
/// The only gateway to the Claude API. Each purpose has its own long-lived SDK client with its model and time
/// budget; the SDK retries <see cref="AnthropicSettings.MaxRetries"/> times with backoff, and the whole call
/// (retries included) is cut off at the purpose's timeout. Failures surface as <see cref="AiProviderException"/>.
/// Logs carry purpose, model, duration and failure kind only — never prompt or response content.
/// </summary>
public sealed class ClaudeClient : IDisposable
{
    private readonly AnthropicSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<ClaudeClient> _logger;
    private readonly HttpMessageHandler? _handler;
    private readonly Lazy<AnthropicClient> _parse;
    private readonly Lazy<AnthropicClient> _summary;

    /// <param name="handler">Test seam: HTTP handler for the SDK; null uses the SDK default.</param>
    public ClaudeClient(
        IOptions<AnthropicSettings> settings, TimeProvider clock, ILogger<ClaudeClient> logger, HttpMessageHandler? handler = null)
    {
        _settings = settings.Value;
        _clock = clock;
        _logger = logger;
        _handler = handler;
        _parse = new(() => CreateClient(_settings.ParseTimeout));
        _summary = new(() => CreateClient(_settings.SummaryTimeout));
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey);

    /// <summary>Runs <paramref name="call"/> with the purpose's client and model inside its time budget.</summary>
    public async Task<T> SendAsync<T>(
        ClaudePurpose purpose,
        Func<AnthropicClient, string, CancellationToken, Task<T>> call,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new AiProviderException(AiFailure.NotConfigured, "The Claude API key is not configured (Anthropic:ApiKey).");
        }

        var (client, model, timeout) = purpose == ClaudePurpose.ResumeParse
            ? (_parse.Value, _settings.ParseModel, _settings.ParseTimeout)
            : (_summary.Value, _settings.SummaryModel, _settings.SummaryTimeout);

        using var budget = new CancellationTokenSource(timeout, _clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);
        var started = _clock.GetTimestamp();
        try
        {
            var result = await call(client, model, linked.Token);
            _logger.LogInformation("Claude {Purpose} ({Model}) answered in {ElapsedMs} ms",
                purpose, model, (int)_clock.GetElapsedTime(started).TotalMilliseconds);
            return result;
        }
        catch (Exception ex) when (Classify(ex, cancellationToken) is { } failure)
        {
            _logger.Log(failure == AiFailure.Rejected ? LogLevel.Error : LogLevel.Warning,
                "Claude {Purpose} ({Model}) failed after {ElapsedMs} ms: {Failure} ({ErrorType})",
                purpose, model, (int)_clock.GetElapsedTime(started).TotalMilliseconds, failure, ex.GetType().Name);
            throw new AiProviderException(failure, $"Claude {purpose} failed: {failure}.", ex);
        }
    }

    /// <summary>Null = not a provider failure (e.g. the caller cancelled); let it propagate unchanged.</summary>
    private static AiFailure? Classify(Exception ex, CancellationToken callerToken) => ex switch
    {
        OperationCanceledException when callerToken.IsCancellationRequested => null,
        OperationCanceledException or TimeoutException => AiFailure.Timeout,
        AnthropicRateLimitException => AiFailure.RateLimited,
        Anthropic5xxException or AnthropicIOException => AiFailure.Unavailable,
        AnthropicApiException => AiFailure.Rejected,
        HttpRequestException => AiFailure.Unavailable,
        _ => null,
    };

    private AnthropicClient CreateClient(TimeSpan timeout) => _handler is null
        ? new AnthropicClient { ApiKey = _settings.ApiKey!, MaxRetries = _settings.MaxRetries, Timeout = timeout }
        : new AnthropicClient
        {
            ApiKey = _settings.ApiKey!,
            MaxRetries = _settings.MaxRetries,
            Timeout = timeout,
            BaseUrl = "https://claude.test",
            HttpClient = new HttpClient(_handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        };

    public void Dispose()
    {
        if (_parse.IsValueCreated)
        {
            (_parse.Value as IDisposable)?.Dispose();
        }

        if (_summary.IsValueCreated)
        {
            (_summary.Value as IDisposable)?.Dispose();
        }
    }
}
