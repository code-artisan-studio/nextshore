using Microsoft.Extensions.Logging;
using NextShore.Application.Abstractions;
using NextShore.Domain.Rules;

namespace NextShore.Infrastructure.Rules;

/// <summary>
/// Process-wide cache of the published rules (singleton). Concurrent first readers share one load; a load that
/// was started before <see cref="Invalidate"/> is never cached, so a publish can't be overwritten by stale rules.
/// No expiry: rules only change through publish, which invalidates.
/// </summary>
public sealed class CachedRuleRepository(IRuleSnapshotSource source, ILogger<CachedRuleRepository> logger)
    : IRuleRepository
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private RuleSnapshot? _snapshot;
    private int _generation;

    public async ValueTask<RuleSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _snapshot) is { } cached)
        {
            return cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref _snapshot) is { } loadedMeanwhile)
            {
                return loadedMeanwhile;
            }

            var generation = Volatile.Read(ref _generation);
            var snapshot = await source.LoadAsync(cancellationToken);
            if (generation == Volatile.Read(ref _generation))
            {
                Volatile.Write(ref _snapshot, snapshot);
            }

            logger.LogInformation(
                "Rule snapshot v{Version} loaded: {Pathways} pathways, {Mappings} language mappings",
                snapshot.Version, snapshot.Pathways.Count, snapshot.LanguageMappings.Count);
            return snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        Volatile.Write(ref _snapshot, null);
        logger.LogInformation("Rule snapshot invalidated");
    }
}
