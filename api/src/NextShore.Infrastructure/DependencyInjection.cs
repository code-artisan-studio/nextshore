using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextShore.Infrastructure.Persistence;
using NextShore.Infrastructure.Seed;

namespace NextShore.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseHealthCheck = "db";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<NextShoreDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<NextShoreDbContext>(DatabaseHealthCheck);
        services.AddScoped<SeedLoader>();

        return services;
    }
}
