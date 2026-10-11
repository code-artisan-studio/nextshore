using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Persistence;

namespace NextShore.Infrastructure.Rules;

public sealed class DbRuleSnapshotSource(IServiceScopeFactory scopes) : IRuleSnapshotSource
{
    public async Task<RuleSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NextShoreDbContext>();

        var countries = await db.Countries.AsNoTracking().ToListAsync(cancellationToken);
        var pathways = await db.Pathways
            .AsNoTracking()
            .Where(p => p.Status == RuleStatus.Published)
            .Include(p => p.NextPathway)
            .Include(p => p.Requirements.Where(r => r.Status == RuleStatus.Published))
            .Include(p => p.PointsFactors.Where(f => f.Status == RuleStatus.Published))
            .Include(p => p.Occupations.Where(o => o.Status == RuleStatus.Published))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var mappings = await db.LanguageScoreMappings
            .AsNoTracking()
            .Where(m => m.Status == RuleStatus.Published)
            .ToListAsync(cancellationToken);

        return RuleSnapshotMapper.Map(countries, pathways, mappings);
    }
}
