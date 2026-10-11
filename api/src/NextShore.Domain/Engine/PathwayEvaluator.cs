using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

public sealed record PathwayEvaluation(
    IReadOnlyList<RequirementOutcome> Requirements, PointsBreakdown? Points, GradeResult Grade);

/// <summary>Requirements → points → grade for one pathway and profile.</summary>
public static class PathwayEvaluator
{
    public static PathwayEvaluation Evaluate(
        PathwayRule pathway, NormalizedProfile profile, ExchangeRates rates, GraderOptions options)
    {
        var requirements = RequirementChecker.CheckAll(pathway, profile, rates);
        var points = PointsCalculator.Calculate(pathway, profile);
        return new PathwayEvaluation(requirements, points, Grader.Evaluate(requirements, points, options));
    }
}
