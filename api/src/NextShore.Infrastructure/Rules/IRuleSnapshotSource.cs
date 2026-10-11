using NextShore.Domain.Rules;

namespace NextShore.Infrastructure.Rules;

/// <summary>Builds a fresh snapshot from storage. Wrapped by <see cref="CachedRuleRepository"/>.</summary>
public interface IRuleSnapshotSource
{
    Task<RuleSnapshot> LoadAsync(CancellationToken cancellationToken);
}
