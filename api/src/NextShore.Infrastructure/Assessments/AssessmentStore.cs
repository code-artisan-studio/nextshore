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

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Assessments.Where(a => a.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
}
