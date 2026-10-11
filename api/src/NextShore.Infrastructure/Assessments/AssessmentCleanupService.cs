using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Application.Assessments;

namespace NextShore.Infrastructure.Assessments;

/// <summary>Deletes expired assessments at startup and then every <see cref="AssessmentSettings.CleanupInterval"/>.</summary>
public sealed class AssessmentCleanupService(
    IServiceScopeFactory scopes,
    IOptions<AssessmentSettings> settings,
    TimeProvider clock,
    ILogger<AssessmentCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.Value.CleanupInterval, clock);
        do
        {
            await CleanupAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<IAssessmentStore>()
                .DeleteExpiredAsync(clock.GetUtcNow(), cancellationToken);
            logger.LogInformation("Deleted {Count} expired assessments", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Try again next interval; never take the API down over cleanup.
            logger.LogError(ex, "Expired assessment cleanup failed");
        }
    }
}
