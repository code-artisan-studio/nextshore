using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

/// <param name="IsProjected">True when evaluated on the profile projected after the earlier steps.</param>
public sealed record ChainStep(
    string PathwaySlug,
    PathwayType Type,
    Grade Grade,
    int ReadinessScore,
    int? DurationMonths,
    bool IsProjected);

/// <param name="TotalMonths">Sum of step durations; null while any duration is unknown.</param>
public sealed record PathwayChain(string CountryCode, IReadOnlyList<ChainStep> Steps, int? TotalMonths);

/// <summary>
/// Study → post-study work → PR chains (VISA-RULES-SPEC.md §5). Each step after the first is graded on a profile
/// projected by the earlier steps. Projections depend only on pathway type and country code:
/// study → next education level and a degree from that country; post-study work → its duration added to work
/// experience; age advances by whole years elapsed. Unknown durations skip the time-based projections.
/// </summary>
public static class ChainBuilder
{
    public static IReadOnlyList<PathwayChain> Build(
        RuleSnapshot snapshot, NormalizedProfile profile, ExchangeRates rates, GraderOptions options) =>
        snapshot.Pathways
            .Where(p => p.Type == PathwayType.Study)
            .Select(start => BuildChain(snapshot, start, profile, rates, options))
            .ToList();

    private static PathwayChain BuildChain(
        RuleSnapshot snapshot, PathwayRule start, NormalizedProfile profile, ExchangeRates rates, GraderOptions options)
    {
        var steps = new List<ChainStep>();
        var visited = new HashSet<string>();
        var projected = profile;
        var startingAge = profile.Get("age") as ProfileValue.Number;
        int? elapsedMonths = 0;

        for (var pathway = start; pathway is not null && visited.Add(pathway.Slug);
             pathway = pathway.NextPathwaySlug is { } next ? snapshot.FindPathway(next) : null)
        {
            var grade = PathwayEvaluator.Evaluate(pathway, projected, rates, options).Grade;
            steps.Add(new ChainStep(pathway.Slug, pathway.Type, grade.Grade, grade.ReadinessScore,
                pathway.TypicalDurationMonths, IsProjected: steps.Count > 0));

            projected = AfterStep(projected, pathway);
            elapsedMonths += pathway.TypicalDurationMonths;
            if (elapsedMonths is { } months && startingAge is not null)
            {
                projected = projected.With("age", new ProfileValue.Number(startingAge.Value + months / 12));
            }
        }

        var durations = steps.Select(s => s.DurationMonths).ToList();
        var total = durations.All(d => d is not null) ? durations.Sum() : null;
        return new PathwayChain(start.CountryCode, steps, total);
    }

    private static NormalizedProfile AfterStep(NormalizedProfile profile, PathwayRule step)
    {
        switch (step.Type)
        {
            case PathwayType.Study:
                if (profile.Get("education.level") is ProfileValue.Ordinal level && level.Rank < level.Scale.Labels.Count - 1)
                {
                    profile = profile.With("education.level", level with { Rank = level.Rank + 1 });
                }

                return profile.With($"degree.recognized_{step.CountryCode}", new ProfileValue.Flag(true));

            case PathwayType.PostStudyWork when step.TypicalDurationMonths is { } months:
                var years = months / 12m;
                profile = AddYears(profile, $"work.years_in_country.{step.CountryCode}", years);
                return AddYears(profile, "work.years_skilled", years);

            default:
                return profile;
        }
    }

    private static NormalizedProfile AddYears(NormalizedProfile profile, string key, decimal years) =>
        profile.With(key, new ProfileValue.Number((profile.Get(key) is ProfileValue.Number n ? n.Value : 0) + years));
}
