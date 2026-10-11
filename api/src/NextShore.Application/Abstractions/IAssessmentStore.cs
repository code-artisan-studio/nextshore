using NextShore.Domain.Entities;

namespace NextShore.Application.Abstractions;

public interface IAssessmentStore
{
    Task AddAsync(Assessment assessment, CancellationToken cancellationToken);

    Task<Assessment?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the summary unless one is already stored (first writer wins) and returns the stored one.
    /// </summary>
    Task<(string Summary, DateTimeOffset GeneratedAt)> SaveSummaryAsync(
        Guid id, string summary, DateTimeOffset generatedAt, CancellationToken cancellationToken);

    /// <summary>Deletes assessments whose <c>expires_at</c> is at or before <paramref name="now"/>; returns how many.</summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
