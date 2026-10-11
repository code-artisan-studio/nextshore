using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NextShore.Api.IntegrationTests.Engine;
using NextShore.Application.Abstractions;
using NextShore.Domain.Rules;

namespace NextShore.Api.IntegrationTests.Api;

/// <summary>
/// The real API with a fixed rule snapshot (verified seed rules by default), a clock fixed at 2026-10-10,
/// the golden-test exchange rates and an in-memory assessment store instead of the database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string UnreachableDb = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2";

    private static readonly Lazy<RuleSnapshot> SeedSnapshot = new(VerifiedSeedSnapshot.Load);

    public RuleSnapshot Snapshot { get; init; } = SeedSnapshot.Value;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero));

    public InMemoryAssessmentStore Assessments { get; } = new();

    /// <summary>Replaces the Claude resume parser when set.</summary>
    public IResumeParser? ResumeParser { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Db", UnreachableDb);
        builder.UseSetting("Engine:ExchangeRates:CAD", "1.37");
        builder.UseSetting("Engine:ExchangeRates:EUR", "0.92");
        builder.UseSetting("Engine:ExchangeRates:AUD", "1.52");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IRuleRepository>(new StaticRuleRepository(Snapshot));
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IAssessmentStore>(Assessments);
            if (ResumeParser is not null)
            {
                services.AddSingleton(ResumeParser);
            }

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
