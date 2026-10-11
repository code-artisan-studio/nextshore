namespace NextShore.Infrastructure.Ai;

/// <summary>Config section <c>Anthropic</c>. The API key comes only from the environment (<c>Anthropic__ApiKey</c>).</summary>
public sealed class AnthropicSettings
{
    public const string Section = "Anthropic";

    public string? ApiKey { get; set; }

    /// <summary>Resume extraction. ARCHITECTURE.md: Haiku.</summary>
    public string ParseModel { get; set; } = "claude-haiku-5-5";

    /// <summary>Results summary. ARCHITECTURE.md: Sonnet.</summary>
    public string SummaryModel { get; set; } = "claude-sonnet-5-5";

    /// <summary>Total time for a parse call, retries included.</summary>
    public TimeSpan ParseTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Total time for a summary call, retries included.</summary>
    public TimeSpan SummaryTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>Retries on 429, 5xx, timeouts of an attempt and connection errors; the SDK backs off between them.</summary>
    public int MaxRetries { get; set; } = 1;
}
