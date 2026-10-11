using NextShore.Application.Results;

namespace NextShore.Application.Abstractions;

public interface ISummaryWriter
{
    /// <summary>A plain-language summary grounded only in the stored profile and engine results.</summary>
    /// <param name="stricter">Second attempt after the first draft broke a guardrail.</param>
    /// <exception cref="AiProviderException">The AI provider failed or declined.</exception>
    Task<string> WriteAsync(string profileJson, EvaluationResponse results, bool stricter, CancellationToken cancellationToken);
}
