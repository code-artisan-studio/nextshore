using NextShore.Application.Common;
using NextShore.Domain.Engine;
using NextShore.Domain.Rules;

namespace NextShore.Application.Results;

public sealed record EvaluationResponse(IReadOnlyList<PathwayResultResponse> Results, IReadOnlyList<PathwayChainResponse> Chains);

public sealed record PathwayResultResponse(
    string PathwaySlug,
    string PathwayName,
    string CountryCode,
    string Grade,
    int ReadinessScore,
    PointsResponse? Points,
    IReadOnlyList<RequirementResponse> Requirements,
    IReadOnlyList<GapResponse> Gaps,
    IReadOnlyList<string> Chain,
    IReadOnlyList<SourceResponse> Sources,
    int StaleRules);

public sealed record PointsResponse(
    string Variant, int Total, int? Target, string? TargetKind, IReadOnlyList<FactorPointsResponse> Breakdown);

public sealed record FactorPointsResponse(string Factor, string Current, int Points);

public sealed record RequirementResponse(
    Guid Id, string Label, bool Met, string Current, string Target, bool IsMandatory, string Fixability);

public sealed record GapResponse(
    Guid? RequirementId,
    string Field,
    string Label,
    string Current,
    string Target,
    string Fixability,
    string FixHint,
    int? FixMonths,
    int ImpactPoints,
    string? UnlocksGrade);

public sealed record SourceResponse(string Label, string Url, DateOnly VerifiedAt);

public sealed record PathwayChainResponse(string CountryCode, int? TotalMonths, IReadOnlyList<ChainStepResponse> Steps);

public sealed record ChainStepResponse(
    string PathwaySlug, string Type, string Grade, int ReadinessScore, int? DurationMonths, bool IsProjected);

public static class EvaluationResponseMapper
{
    public static EvaluationResponse From(EngineResult result, RuleSnapshot snapshot) => new(
        result.Results.Select(r => new PathwayResultResponse(
            r.PathwaySlug,
            snapshot.FindPathway(r.PathwaySlug)?.Name ?? r.PathwaySlug,
            r.CountryCode,
            EnumText.Of(r.Grade),
            r.ReadinessScore,
            r.Points is { } p
                ? new PointsResponse(EnumText.Of(p.Variant), p.Total, p.Target,
                    p.TargetKind is { } kind ? EnumText.Of(kind) : null,
                    p.Breakdown.Select(f => new FactorPointsResponse(f.Factor, f.Current, f.Points)).ToList())
                : null,
            r.Requirements.Select(q => new RequirementResponse(
                q.RequirementId, q.Label, q.Met, q.Current, q.Target, q.IsMandatory, EnumText.Of(q.Fixability))).ToList(),
            r.Gaps.Select(g => new GapResponse(
                g.RequirementId, g.Field, g.Label, g.Current, g.Target, EnumText.Of(g.Fixability), g.FixHint, g.FixMonths,
                g.ImpactPoints, g.UnlocksGrade is { } grade ? EnumText.Of(grade) : null)).ToList(),
            r.Chain,
            r.Sources.Select(s => new SourceResponse(s.Label, s.Url, s.VerifiedAt)).ToList(),
            r.StaleRules)).ToList(),
        result.Chains.Select(c => new PathwayChainResponse(
            c.CountryCode,
            c.TotalMonths,
            c.Steps.Select(s => new ChainStepResponse(
                s.PathwaySlug, EnumText.Of(s.Type), EnumText.Of(s.Grade), s.ReadinessScore, s.DurationMonths, s.IsProjected)).ToList()))
            .ToList());
}
