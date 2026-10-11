using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Options;
using NextShore.Application.Abstractions;
using NextShore.Application.Engine;
using NextShore.Application.Profiles;
using NextShore.Application.Results;
using NextShore.Domain;
using NextShore.Domain.Entities;

namespace NextShore.Application.Assessments;

public sealed record AssessmentResponse(
    Guid Id,
    int RuleVersion,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<PathwayResultResponse> Results,
    IReadOnlyList<PathwayChainResponse> Chains);

internal static class AssessmentJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>
/// <c>POST /assessments</c>: evaluates and stores the user-confirmed profile with its results. Nothing else about
/// the user is stored; the row is deleted after <see cref="AssessmentSettings.LifetimeDays"/>.
/// </summary>
public sealed class CreateAssessmentHandler(
    IValidator<CandidateProfileRequest> validator,
    EngineRunner engine,
    IAssessmentStore store,
    IOptions<AssessmentSettings> settings,
    TimeProvider clock)
{
    public async Task<Result<AssessmentResponse>> HandleAsync(CandidateProfileRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.ToDictionary().AsReadOnly());
        }

        var (result, snapshot) = await engine.RunAsync(CandidateProfileMapper.ToDomain(request), cancellationToken);
        var evaluation = EvaluationResponseMapper.From(result, snapshot);
        var now = clock.GetUtcNow();
        var assessment = new Assessment
        {
            Id = Guid.CreateVersion7(now),
            Profile = JsonSerializer.Serialize(request, AssessmentJson.Options),
            Results = JsonSerializer.Serialize(evaluation, AssessmentJson.Options),
            RuleVersion = result.RuleVersion,
            CreatedAt = now,
            ExpiresAt = now.AddDays(settings.Value.LifetimeDays),
        };
        await store.AddAsync(assessment, cancellationToken);

        return new AssessmentResponse(assessment.Id, assessment.RuleVersion, assessment.ExpiresAt, evaluation.Results, evaluation.Chains);
    }
}

/// <summary><c>GET /assessments/{id}</c>: the results as stored, so a shared link shows what the user saw.</summary>
public sealed class GetAssessmentHandler(IAssessmentStore store, TimeProvider clock)
{
    public async Task<Result<AssessmentResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var assessment = await store.FindAsync(id, cancellationToken);
        if (assessment is null || assessment.ExpiresAt <= clock.GetUtcNow())
        {
            return Error.NotFound("This assessment does not exist or has expired.");
        }

        var evaluation = JsonSerializer.Deserialize<EvaluationResponse>(assessment.Results, AssessmentJson.Options)!;
        return new AssessmentResponse(assessment.Id, assessment.RuleVersion, assessment.ExpiresAt, evaluation.Results, evaluation.Chains);
    }
}
