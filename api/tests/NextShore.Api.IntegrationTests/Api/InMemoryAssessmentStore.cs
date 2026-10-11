using System.Collections.Concurrent;
using NextShore.Application.Abstractions;
using NextShore.Domain.Entities;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class InMemoryAssessmentStore : IAssessmentStore
{
    public ConcurrentDictionary<Guid, Assessment> Rows { get; } = new();

    public List<DateTimeOffset> CleanupCalls { get; } = [];

    public Task AddAsync(Assessment assessment, CancellationToken cancellationToken)
    {
        Rows[assessment.Id] = assessment;
        return Task.CompletedTask;
    }

    public Task<Assessment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.GetValueOrDefault(id));

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var expired = Rows.Values.Where(a => a.ExpiresAt <= now).ToList();
        expired.ForEach(a => Rows.TryRemove(a.Id, out _));
        lock (CleanupCalls)
        {
            CleanupCalls.Add(now); // recorded after deleting, so waiting on it observes the result
        }

        return Task.FromResult(expired.Count);
    }
}
