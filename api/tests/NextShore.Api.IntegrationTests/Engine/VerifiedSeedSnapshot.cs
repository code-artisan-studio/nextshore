using NextShore.Domain.Rules;
using NextShore.Infrastructure.Rules;

namespace NextShore.Api.IntegrationTests.Engine;

/// <summary>The real api/seed rules with every verified row treated as published (see <see cref="SeedPreviewSnapshotSource"/>).</summary>
internal static class VerifiedSeedSnapshot
{
    public static RuleSnapshot Load() => SeedPreviewSnapshotSource.Build(SeedPaths.Directory);
}
