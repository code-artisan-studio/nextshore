using Microsoft.EntityFrameworkCore;
using NextShore.Application.Abstractions;
using NextShore.Domain.Entities;
using NextShore.Infrastructure.Persistence;

namespace NextShore.Infrastructure.Assessments;

public sealed class AssessmentStore(NextShoreDbContext db) : IAssessmentStore
{
    public async Task AddAsync(Assessment assessment, CancellationToken cancellationToken)
    {
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Assessment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Assessments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(string Summary, DateTimeOffset GeneratedAt)> SaveSummaryAsync(
        Guid id, string summary, DateTimeOffset generatedAt, CancellationToken cancellationToken)
    {
        await db.Assessments
            .Where(a => a.Id == id && a.Summary == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(a => a.Summary, summary)
                .SetProperty(a => a.SummaryGeneratedAt, generatedAt), cancellationToken);

        var stored = await db.Assessments.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.Summary, a.SummaryGeneratedAt })
            .SingleAsync(cancellationToken);
        return (stored.Summary!, stored.SummaryGeneratedAt!.Value);
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Assessments.Where(a => a.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
}
