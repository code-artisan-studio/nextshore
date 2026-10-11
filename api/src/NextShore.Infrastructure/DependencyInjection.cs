using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NextShore.Application.Abstractions;
using NextShore.Infrastructure.Ai;
using NextShore.Infrastructure.Assessments;
using NextShore.Infrastructure.Persistence;
using NextShore.Infrastructure.Rules;
using NextShore.Infrastructure.Seed;

namespace NextShore.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseHealthCheck = "db";
    public const string SeedPreviewRules = "seed-preview";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        var connectionString = config.GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<NextShoreDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<NextShoreDbContext>(DatabaseHealthCheck);
        services.AddScoped<SeedLoader>();
        services.AddScoped<IAssessmentStore, AssessmentStore>();
        services.AddOptions<AnthropicSettings>()
            .Bind(config.GetSection(AnthropicSettings.Section))
            .Validate(s => !string.IsNullOrWhiteSpace(s.ParseModel) && !string.IsNullOrWhiteSpace(s.SummaryModel)
                    && s.ParseTimeout > TimeSpan.Zero && s.SummaryTimeout > TimeSpan.Zero && s.MaxRetries >= 0,
                "Anthropic: models must be set, timeouts positive and MaxRetries >= 0.")
            .ValidateOnStart();
        services.AddSingleton<ClaudeClient>();
        services.AddHostedService<AssessmentCleanupService>();
        if (string.Equals(config["Rules:Source"], SeedPreviewRules, StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Rules:Source=seed-preview serves unpublished rules and is only allowed in Development.");
            }

            var seedDirectory = Path.GetFullPath(config["Rules:SeedPath"]
                ?? Path.Combine(environment.ContentRootPath, "..", "..", "seed"));
            services.AddSingleton<IRuleSnapshotSource>(new SeedPreviewSnapshotSource(seedDirectory));
        }
        else
        {
            services.AddSingleton<IRuleSnapshotSource, DbRuleSnapshotSource>();
        }

        services.AddSingleton<IRuleRepository, CachedRuleRepository>();

        return services;
    }
}
