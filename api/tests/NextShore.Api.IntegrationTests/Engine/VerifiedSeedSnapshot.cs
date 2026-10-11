using NextShore.Domain.Rules;
using NextShore.Infrastructure.Rules;

namespace NextShore.Api.IntegrationTests.Engine;

/// <summary>The real api/seed rules with every verified row treated as published (see <see cref="SeedPreviewSnapshotSource"/>).</summary>
internal static class VerifiedSeedSnapshot
{
    public static RuleSnapshot Load() => SeedPreviewSnapshotSource.Build(FindSeedDirectory());

    private static string FindSeedDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NextShore.sln")))
            {
                return Path.Combine(dir.FullName, "seed");
            }
        }

        throw new DirectoryNotFoundException("api/seed not found above the test output directory.");
    }
}
