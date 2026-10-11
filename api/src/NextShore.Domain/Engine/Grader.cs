using NextShore.Domain.Enums;

namespace NextShore.Domain.Engine;

/// <summary>Config <c>Engine:NearEligibleMaxGaps</c> and <c>Engine:NearEligiblePointsRatio</c>.</summary>
public sealed record GraderOptions(int NearEligibleMaxGaps = 2, decimal NearEligiblePointsRatio = 0.85m)
{
    public static GraderOptions Default { get; } = new();
}

public sealed record GradeResult(Grade Grade, int ReadinessScore);

/// <summary>Grade and Readiness Score per VISA-RULES-SPEC.md §2–3. Advisory requirements never count.</summary>
public static class Grader
{
    public const int RequirementsWeight = 60;
    public const int PointsWeight = 40;
    public const int ImpossibleScoreCap = 20;

    public static GradeResult Evaluate(
        IReadOnlyList<RequirementOutcome> requirements, PointsBreakdown? points, GraderOptions options)
    {
        var mandatory = requirements.Where(r => r.IsMandatory).ToList();
        var unmet = mandatory.Where(r => !r.Met).ToList();
        var hasImpossibleGap = unmet.Any(r => r.Fixability == Fixability.Impossible);

        // No usable target (none published, or 0) means points cannot block or reduce the grade.
        var target = points?.Target is > 0 ? points.Target : null;
        var pointsRatio = points is not null && target is { } t ? Math.Min((decimal)points.Total / t, 1m) : 1m;
        var requirementsRatio = mandatory.Count == 0 ? 1m : (decimal)(mandatory.Count - unmet.Count) / mandatory.Count;

        var score = (int)Math.Round(
            RequirementsWeight * requirementsRatio + PointsWeight * pointsRatio, MidpointRounding.AwayFromZero);
        if (hasImpossibleGap)
        {
            score = Math.Min(score, ImpossibleScoreCap);
        }

        var meetsTarget = target is null || points!.Total >= target;
        var nearTarget = target is null || points!.Total >= options.NearEligiblePointsRatio * target.Value;

        var grade = unmet.Count == 0 && meetsTarget ? Grade.Eligible
            : !hasImpossibleGap && unmet.Count <= options.NearEligibleMaxGaps && nearTarget ? Grade.NearEligible
            : Grade.NotEligible;

        return new GradeResult(grade, score);
    }
}
