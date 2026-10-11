using NextShore.Domain.Entities;

namespace NextShore.Application.Abstractions;

public interface IAssessmentStore
{
    Task AddAsync(Assessment assessment, CancellationToken cancellationToken);

    Task<Assessment?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Deletes assessments whose <c>expires_at</c> is at or before <paramref name="now"/>; returns how many.</summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
