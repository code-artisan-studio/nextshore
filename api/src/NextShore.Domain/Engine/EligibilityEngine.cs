using NextShore.Domain.Enums;
using NextShore.Domain.Profiles;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

/// <param name="StaleAfterDays">Rows verified longer ago than this count as stale (spec §9).</param>
public sealed record EngineOptions(GraderOptions Grading, int StaleAfterDays = 90)
{
    public static EngineOptions Default { get; } = new(GraderOptions.Default);
}

public sealed record SourceRef(string Label, string Url, DateOnly VerifiedAt);

/// <param name="Chain">Slugs of the study chain this pathway is part of, or just this pathway.</param>
/// <param name="StaleRules">Rows of this pathway verified more than <see cref="EngineOptions.StaleAfterDays"/> ago.</param>
public sealed record PathwayResult(
    string PathwaySlug,
    string CountryCode,
    Grade Grade,
    int ReadinessScore,
    PointsBreakdown? Points,
    IReadOnlyList<RequirementOutcome> Requirements,
    IReadOnlyList<Gap> Gaps,
    IReadOnlyList<string> Chain,
    IReadOnlyList<SourceRef> Sources,
    int StaleRules);

public sealed record EngineResult(int RuleVersion, IReadOnlyList<PathwayResult> Results, IReadOnlyList<PathwayChain> Chains);

/// <summary>
/// The deterministic engine (ARCHITECTURE.md): normalise → filter by audience → evaluate every pathway
/// (requirements, points, grade, gaps) → chains. Pure: same inputs, same output.
/// </summary>
public static class EligibilityEngine
{
    public static EngineResult Evaluate(
        CandidateProfile profile, RuleSnapshot snapshot, ExchangeRates rates, EngineOptions options, DateOnly today) =>
        Evaluate(ProfileNormalizer.Normalize(profile, snapshot.LanguageMappings, today), snapshot, rates, options, today);

    public static EngineResult Evaluate(
        NormalizedProfile profile, RuleSnapshot snapshot, ExchangeRates rates, EngineOptions options, DateOnly today)
    {
        var audience = profile.Get("audience") is ProfileValue.Text { Value: var a }
            && Enum.TryParse<Audience>(a, ignoreCase: true, out var parsed) ? parsed : (Audience?)null;
        bool ForAudience(PathwayRule p) => audience is { } who && p.Audience.Contains(who);

        // Chains start at the study pathways this audience sees; students also see every step of their chains.
        var visibleSnapshot = new RuleSnapshot(snapshot.Version, snapshot.Countries,
            snapshot.Pathways.Where(p => p.Type != PathwayType.Study || ForAudience(p)).ToList(), snapshot.LanguageMappings);
        var chains = ChainBuilder.Build(visibleSnapshot, profile, rates, options.Grading);
        var chainBySlug = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var slugs in chains.Select(c => c.Steps.Select(s => s.PathwaySlug).ToList()))
        {
            foreach (var slug in slugs)
            {
                chainBySlug.TryAdd(slug, slugs);
            }
        }

        var staleBefore = today.AddDays(-options.StaleAfterDays);
        var results = snapshot.Pathways
            .Where(p => ForAudience(p) || chainBySlug.ContainsKey(p.Slug))
            .Select(pathway =>
            {
                var evaluation = PathwayEvaluator.Evaluate(pathway, profile, rates, options.Grading);
                return new PathwayResult(
                    pathway.Slug,
                    pathway.CountryCode,
                    evaluation.Grade.Grade,
                    evaluation.Grade.ReadinessScore,
                    evaluation.Points,
                    evaluation.Requirements,
                    GapBuilder.Build(pathway, profile, rates, options.Grading),
                    chainBySlug.GetValueOrDefault(pathway.Slug) ?? [pathway.Slug],
                    Sources(pathway),
                    SourcedRows(pathway).Count(row => row.Source.VerifiedAt < staleBefore));
            })
            .OrderByDescending(r => r.ReadinessScore)
            .ThenBy(r => r.PathwaySlug, StringComparer.Ordinal)
            .ToList();

        return new EngineResult(snapshot.Version, results, chains);
    }

    /// <summary>One entry per URL, labelled by the first row using it, dated by its oldest verification.</summary>
    private static IReadOnlyList<SourceRef> Sources(PathwayRule pathway) =>
        SourcedRows(pathway)
            .GroupBy(row => row.Source.Url)
            .Select(g => new SourceRef(g.First().Label, g.Key, g.Min(row => row.Source.VerifiedAt)))
            .ToList();

    private static IEnumerable<(string Label, RuleSource Source)> SourcedRows(PathwayRule pathway) =>
        new[] { (pathway.Name, pathway.Source) }
            .Concat(pathway.Requirements.Select(r => (r.Label, r.Source)))
            .Concat(pathway.PointsFactors.Select(f => ($"{pathway.Name} points", f.Source)))
            .Concat(pathway.Occupations.Select(o => ($"{pathway.Name} occupations", o.Source)));
}
