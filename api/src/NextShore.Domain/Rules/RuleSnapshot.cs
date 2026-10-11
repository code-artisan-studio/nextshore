using NextShore.Domain.Enums;

namespace NextShore.Domain.Rules;

/// <summary>
/// Immutable view of every published rule — the only rule input the eligibility engine sees.
/// <see cref="Version"/> is the highest published <c>rule_version</c>; publishing sets a pathway's version to
/// that maximum + 1, so any publish yields a new snapshot version.
/// </summary>
public sealed class RuleSnapshot
{
    private readonly Dictionary<string, PathwayRule> _pathwaysBySlug;

    public RuleSnapshot(
        int version,
        IReadOnlyList<CountryRule> countries,
        IReadOnlyList<PathwayRule> pathways,
        IReadOnlyList<LanguageMappingRule> languageMappings)
    {
        Version = version;
        Countries = countries;
        Pathways = pathways;
        LanguageMappings = languageMappings;
        _pathwaysBySlug = pathways.ToDictionary(p => p.Slug);
    }

    public static RuleSnapshot Empty { get; } = new(0, [], [], []);

    public int Version { get; }
    public IReadOnlyList<CountryRule> Countries { get; }
    public IReadOnlyList<PathwayRule> Pathways { get; }
    public IReadOnlyList<LanguageMappingRule> LanguageMappings { get; }

    public PathwayRule? FindPathway(string slug) => _pathwaysBySlug.GetValueOrDefault(slug);
}

public sealed record RuleSource(string Url, DateOnly VerifiedAt);

public sealed record CountryRule(string Code, string Name, string OccupationSystem, string LanguageScale);

public sealed record PathwayRule(
    string Slug,
    string CountryCode,
    string Name,
    PathwayType Type,
    IReadOnlyList<Audience> Audience,
    bool IsPointsBased,
    int? PassMark,
    int? Cutoff,
    DateOnly? CutoffAsOf,
    string? NextPathwaySlug,
    int? TypicalDurationMonths,
    string Summary,
    int RuleVersion,
    RuleSource Source,
    IReadOnlyList<RequirementRule> Requirements,
    IReadOnlyList<PointsFactorRule> PointsFactors,
    IReadOnlyList<OccupationRule> Occupations);

/// <param name="Value">Raw JSON (scalar or array); null for <c>exists</c> / <c>true</c>.</param>
public sealed record RequirementRule(
    Guid Id,
    string Field,
    RequirementOperator Operator,
    string? Value,
    string? Unit,
    bool IsMandatory,
    Fixability Fixability,
    string FixHint,
    int? FixMonths,
    string Label,
    RuleSource Source);

public sealed record PointsFactorRule(
    string Factor,
    PointsVariant Variant,
    decimal? Min,
    decimal? Max,
    string? MatchValue,
    int Points,
    int? MaxPointsForFactor,
    RuleSource Source);

public sealed record OccupationRule(string Code, string Title, bool IsEligible, RuleSource Source);

public sealed record LanguageMappingRule(
    string Test,
    string? TestVariant,
    string Skill,
    decimal MinScore,
    string TargetScale,
    string TargetLevel,
    RuleSource Source);
