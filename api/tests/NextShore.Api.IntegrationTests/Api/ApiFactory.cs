using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NextShore.Api.IntegrationTests.Engine;
using NextShore.Application.Abstractions;
using NextShore.Domain.Rules;

namespace NextShore.Api.IntegrationTests.Api;

/// <summary>The real API with a fixed rule snapshot (verified seed rules by default) and no database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string UnreachableDb = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2";

    private static readonly Lazy<RuleSnapshot> SeedSnapshot = new(VerifiedSeedSnapshot.Load);

    public RuleSnapshot Snapshot { get; init; } = SeedSnapshot.Value;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Db", UnreachableDb);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IRuleRepository>(new StaticRuleRepository(Snapshot));
            ConfigureServices(services);
        });
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private sealed class StaticRuleRepository(RuleSnapshot snapshot) : IRuleRepository
    {
        public ValueTask<RuleSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) => new(snapshot);

        public void Invalidate()
        {
        }
    }
}
