using NextShore.Domain.Rules;

namespace NextShore.Application.Abstractions;

public interface IRuleRepository
{
    /// <summary>The current published rules, served from cache after the first load.</summary>
    ValueTask<RuleSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops the cached snapshot; the next read reloads. Call after every publish.</summary>
    void Invalidate();
}
