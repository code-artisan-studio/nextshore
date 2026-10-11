using System.Text.Json;

namespace NextShore.Infrastructure.Seed;

// Shapes of api/seed/*.json (VISA-RULES-SPEC.md §7). Any visa value typed JsonElement may be "TODO_VERIFY".
// sourceUrl/verifiedAt on a child row default to the pathway's.

public sealed record CountrySeedFile(CountrySeed Country, List<PathwaySeed> Pathways);

public sealed record CountrySeed(string Code, string Name, string OccupationSystem, string LanguageScale);

public sealed record PathwaySeed(
    string Slug,
    string Name,
    string Type,
    List<string> Audience,
    bool IsPointsBased,
    string Summary,
    string SourceUrl,
    JsonElement? PassMark = null,
    JsonElement? Cutoff = null,
    JsonElement? CutoffAsOf = null,
    string? NextPathwaySlug = null,
    JsonElement? TypicalDurationMonths = null,
    DateOnly? VerifiedAt = null,
    List<RequirementSeed>? Requirements = null,
    List<PointsFactorSeed>? PointsFactors = null,
    List<OccupationSeed>? Occupations = null);

public sealed record RequirementSeed(
    string Field,
    string Operator,
    JsonElement Value,
    bool IsMandatory,
    string Fixability,
    string FixHint,
    string Label,
    string? Unit = null,
    int? FixMonths = null,
    string? SourceUrl = null,
    DateOnly? VerifiedAt = null);

public sealed record PointsFactorSeed(
    string Factor,
    string Variant,
    JsonElement Points,
    JsonElement? Min = null,
    JsonElement? Max = null,
    JsonElement? MatchValue = null,
    JsonElement? MaxPointsForFactor = null,
    string? SourceUrl = null,
    DateOnly? VerifiedAt = null);

public sealed record OccupationSeed(
    string Code,
    string Title,
    JsonElement IsEligible,
    string? SourceUrl = null,
    DateOnly? VerifiedAt = null);

public sealed record LanguageMappingsSeedFile(List<LanguageMappingGroupSeed> Groups);

/// <summary>Minimum score per skill needed for each level of the target scale.</summary>
public sealed record LanguageMappingGroupSeed(
    string Test,
    string TargetScale,
    string SourceUrl,
    List<LanguageLevelSeed> Levels,
    DateOnly? VerifiedAt = null);

public sealed record LanguageLevelSeed(string TargetLevel, Dictionary<string, JsonElement> MinScores);
