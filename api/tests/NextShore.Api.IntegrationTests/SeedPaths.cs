namespace NextShore.Api.IntegrationTests;

internal static class SeedPaths
{
    /// <summary>api/seed, found by walking up from the test output to NextShore.sln.</summary>
    public static string Directory { get; } = Find();

    private static string Find()
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
