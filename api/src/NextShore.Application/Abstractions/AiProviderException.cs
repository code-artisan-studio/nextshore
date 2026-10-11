namespace NextShore.Application.Abstractions;

public enum AiFailure
{
    /// <summary><c>Anthropic:ApiKey</c> is not set.</summary>
    NotConfigured,

    /// <summary>No answer within the purpose's time budget (retries included).</summary>
    Timeout,

    /// <summary>429 after the retry.</summary>
    RateLimited,

    /// <summary>5xx or network failure after the retry.</summary>
    Unavailable,

    /// <summary>The request was refused (4xx other than 429), e.g. bad key or invalid request.</summary>
    Rejected,
}

/// <summary>The AI provider could not answer. Endpoints map this to 502; nothing about the request content is included.</summary>
public sealed class AiProviderException(AiFailure failure, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public AiFailure Failure { get; } = failure;
}
