using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Domain.Engine;
using NextShore.Domain.Profiles;
using NextShore.Domain.Rules;

namespace NextShore.Application.Engine;

/// <summary>Runs the Domain engine on the current published rules with configured settings and today's date.</summary>
public sealed class EngineRunner(IRuleRepository rules, IOptions<EngineSettings> settings, TimeProvider clock)
{
    public async Task<(EngineResult Result, RuleSnapshot Snapshot)> RunAsync(
        CandidateProfile profile, CancellationToken cancellationToken)
    {
        var snapshot = await rules.GetSnapshotAsync(cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var result = EligibilityEngine.Evaluate(
            profile, snapshot, settings.Value.ToExchangeRates(), settings.Value.ToEngineOptions(), today);
        return (result, snapshot);
    }
}
