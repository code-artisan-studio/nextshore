using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Infrastructure.Rules;

/// <summary>Builds a <see cref="RuleSnapshot"/> from published rows. Fails loudly on a row publish should have refused.</summary>
public static class RuleSnapshotMapper
{
    public static RuleSnapshot Map(
        IReadOnlyCollection<Country> countries,
        IReadOnlyCollection<Pathway> pathways,
        IReadOnlyCollection<LanguageScoreMapping> mappings)
    {
        var published = pathways.Where(p => p.Status == RuleStatus.Published).ToList();
        var publishedSlugs = published.Select(p => p.Slug).ToHashSet();
        var countryCodes = countries.ToDictionary(c => c.Id, c => c.Code);

        var pathwayRules = published
            .OrderBy(p => p.Slug, StringComparer.Ordinal)
            .Select(p => new PathwayRule(
                p.Slug,
                countryCodes.GetValueOrDefault(p.CountryId)
                    ?? throw Invalid($"pathway {p.Slug}", "country not found"),
                p.Name,
                p.Type,
                p.Audience.ToArray(),
                p.IsPointsBased,
                p.PassMark,
                p.Cutoff,
                p.CutoffAsOf,
                // A chain only continues into another published pathway.
                p.NextPathway is { } next && publishedSlugs.Contains(next.Slug) ? next.Slug : null,
                p.TypicalDurationMonths,
                p.Summary,
                p.RuleVersion,
                Source(p, $"pathway {p.Slug}"),
                p.Requirements.Where(IsPublished).Select(r => new RequirementRule(
                    r.Id, r.Field, r.Operator, r.Value, r.Unit, r.IsMandatory, r.Fixability, r.FixHint, r.FixMonths,
                    r.Label, Source(r, $"{p.Slug} requirement '{r.Label}'"))).ToArray(),
                p.PointsFactors.Where(IsPublished).Select(f => new PointsFactorRule(
                    f.Factor, f.Variant, f.Min, f.Max, f.MatchValue,
                    f.Points ?? throw Invalid($"{p.Slug} points factor {f.Factor}", "points is null"),
                    f.MaxPointsForFactor, Source(f, $"{p.Slug} points factor {f.Factor}"))).ToArray(),
                p.Occupations.Where(IsPublished).Select(o => new OccupationRule(
                    o.Code, o.Title, o.IsEligible, Source(o, $"{p.Slug} occupation {o.Code}"))).ToArray()))
            .ToArray();

        var mappingRules = mappings
            .Where(IsPublished)
            .Select(m =>
            {
                var label = $"language mapping {m.Test} {m.Skill} → {m.TargetScale} {m.TargetLevel}";
                return new LanguageMappingRule(
                    m.Test, m.TestVariant, m.Skill,
                    m.MinScore ?? throw Invalid(label, "min_score is null"),
                    m.TargetScale, m.TargetLevel, Source(m, label));
            })
            .ToArray();

        // Countries have no publish flow of their own; every country is available.
        var countryRules = countries
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .Select(c => new CountryRule(c.Code, c.Name, c.OccupationSystem, c.LanguageScale))
            .ToArray();

        var version = pathwayRules.Length == 0 ? 0 : pathwayRules.Max(p => p.RuleVersion);
        return new RuleSnapshot(version, countryRules, pathwayRules, mappingRules);
    }

    private static bool IsPublished(RuleEntity rule) => rule.Status == RuleStatus.Published;

    private static RuleSource Source(SourcedRuleEntity rule, string label)
    {
        if (rule.UnverifiedFields.Count > 0)
        {
            throw Invalid(label, $"{RuleValues.TodoVerify}: {string.Join(", ", rule.UnverifiedFields)}");
        }

        return new RuleSource(rule.SourceUrl, rule.VerifiedAt ?? throw Invalid(label, "verified_at is null"));
    }

    private static InvalidOperationException Invalid(string label, string reason) =>
        new($"Published rule is not usable ({label}): {reason}.");
}
