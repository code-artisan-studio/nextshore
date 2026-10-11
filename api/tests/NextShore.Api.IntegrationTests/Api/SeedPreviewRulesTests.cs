using Microsoft.AspNetCore.Hosting;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class SeedPreviewRulesTests
{
    [Fact]
    public void Seed_preview_rules_are_refused_outside_development()
    {
        using var factory = new ApiFactory().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Rules:Source", "seed-preview"));

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("only allowed in Development", ex.GetBaseException().Message);
    }
}
