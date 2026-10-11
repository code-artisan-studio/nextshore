using System.Text.Json;
using Microsoft.Extensions.Logging;
using NextShore.Application.Abstractions;
using NextShore.Application.Results;
using NextShore.Domain;

namespace NextShore.Application.Assessments;

public sealed record SummaryResponse(string Summary, DateTimeOffset GeneratedAt);

/// <summary>
/// <c>GET /assessments/{id}/summary</c>: generated once from the stored profile and results (never re-evaluated),
/// checked by <see cref="SummaryGuard"/> (one retry), then cached on the assessment.
/// </summary>
public sealed class GetSummaryHandler(
    IAssessmentStore store, ISummaryWriter writer, TimeProvider clock, ILogger<GetSummaryHandler> logger)
{
    public async Task<Result<SummaryResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var assessment = await store.FindAsync(id, cancellationToken);
        if (assessment is null || assessment.ExpiresAt <= clock.GetUtcNow())
        {
            return Error.NotFound("This assessment does not exist or has expired.");
        }

        if (assessment is { Summary: { } cached, SummaryGeneratedAt: { } cachedAt })
        {
            return new SummaryResponse(cached, cachedAt);
        }

        var results = JsonSerializer.Deserialize<EvaluationResponse>(assessment.Results, AssessmentJson.Options)!;
        try
        {
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var draft = await writer.WriteAsync(assessment.Profile, results, stricter: attempt > 1, cancellationToken);
                var banned = SummaryGuard.FindBannedPhrase(draft);
                var fitted = banned is null ? SummaryGuard.FitToLimit(draft) : null;
                if (fitted is not null)
                {
                    var (summary, generatedAt) = await store.SaveSummaryAsync(id, fitted, clock.GetUtcNow(), cancellationToken);
                    return new SummaryResponse(summary, generatedAt);
                }

                // Guardrail outcome only — never the text.
                logger.LogWarning("Summary draft {Attempt} rejected: {Reason}", attempt,
                    banned is null ? "over the word limit with no sentence break" : "banned phrase");
            }
        }
        catch (AiProviderException ex)
        {
            logger.LogWarning("Summary generation failed: {Failure} {Reason}", ex.Failure, ex.Message);
        }

        return Error.Upstream("The summary is unavailable right now. Your results are complete without it; try again shortly.");
    }
}
