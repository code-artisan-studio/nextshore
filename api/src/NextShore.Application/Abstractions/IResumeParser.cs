using NextShore.Application.Resumes;

namespace NextShore.Application.Abstractions;

public enum ResumeFormat
{
    Pdf,
    Docx,
}

/// <summary>An uploaded resume, held in memory only for the length of the request.</summary>
public sealed record ResumeDocument(ResumeFormat Format, byte[] Content);

public interface IResumeParser
{
    /// <exception cref="ResumeUnreadableException">The document has no readable content or the model declined it.</exception>
    /// <exception cref="AiProviderException">The AI provider failed.</exception>
    Task<ExtractedProfile> ParseAsync(ResumeDocument document, CancellationToken cancellationToken);
}

public sealed class ResumeUnreadableException(string message, Exception? inner = null) : Exception(message, inner);
