using System.Text.Json;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

/// <param name="RequirementId">Null for points gaps.</param>
/// <param name="Field">Profile field key the fix changes, e.g. <c>english.level.CLB.writing</c>.</param>
/// <param name="UnlocksGrade">Grade after applying only this fix; null when it cannot be simulated.</param>
public sealed record Gap(
    Guid? RequirementId,
    string Field,
    string Label,
    string Current,
    string Target,
    Fixability Fixability,
    string FixHint,
    int? FixMonths,
    int ImpactPoints,
    Grade? UnlocksGrade);

/// <summary>
/// What blocks a pathway and what each fix is worth (VISA-RULES-SPEC.md §4). Every fix is simulated by
/// re-running the engine on a profile with only that change.
/// </summary>
public static class GapBuilder
{
    public const int MaxPointsGaps = 3;
    public const string PointsField = "points";

    public static IReadOnlyList<Gap> Build(
        PathwayRule pathway, NormalizedProfile profile, ExchangeRates rates, GraderOptions options)
    {
        PathwayEvaluation Evaluate(NormalizedProfile p) => PathwayEvaluator.Evaluate(pathway, p, rates, options);

        var current = Evaluate(profile);
        int Impact(PathwayEvaluation after) => (after.Points?.Total ?? 0) - (current.Points?.Total ?? 0);

        var gaps = new List<Gap>();
        foreach (var (rule, outcome) in pathway.Requirements.Zip(current.Requirements).Where(x => x.Second.IsMandatory && !x.Second.Met))
        {
            var fixedValue = outcome.Fixability == Fixability.Impossible
                ? null
                : SatisfyingValue(rule, profile.Get(rule.Field), rates);
            var after = fixedValue is null ? null : Evaluate(ProfileAdjust.Set(profile, rule.Field, fixedValue));
            gaps.Add(new Gap(rule.Id, rule.Field, outcome.Label, outcome.Current, outcome.Target, outcome.Fixability,
                outcome.FixHint, outcome.FixMonths, after is null ? 0 : Impact(after), after?.Grade.Grade));
        }

        if (current.Points is { Target: > 0 and var target } points && points.Total < target)
        {
            var improvements = PointsImprovements(pathway, profile, points, Evaluate, Impact)
                .OrderByDescending(g => g.ImpactPoints)
                .ThenBy(g => g.FixMonths ?? int.MaxValue)
                .Take(MaxPointsGaps)
                .ToList();
            gaps.AddRange(improvements.Count > 0
                ? improvements
                : [new Gap(null, PointsField, "Points", $"{points.Total} points", $"{target} points", Fixability.Hard,
                    "No single change closes the points gap", null, 0, null)]);
        }

        return gaps
            .OrderBy(g => g.Fixability == Fixability.Impossible)
            .ThenByDescending(g => (decimal)g.ImpactPoints / Math.Max(g.FixMonths ?? 1, 1))
            .ThenBy(g => g.Fixability)
            .ThenBy(g => g.FixMonths ?? int.MaxValue)
            .ThenBy(g => g.Label, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>For each improvable factor, the next bracket or level that scores more, as a simulated gap.</summary>
    private static IEnumerable<Gap> PointsImprovements(
        PathwayRule pathway, NormalizedProfile profile, PointsBreakdown points,
        Func<NormalizedProfile, PathwayEvaluation> evaluate, Func<PathwayEvaluation, int> impact)
    {
        var rows = pathway.PointsFactors.Where(r => r.Variant == points.Variant).ToList();
        foreach (var factor in points.Breakdown)
        {
            if (factor.Factor == "age")
            {
                continue; // not something the user can change
            }

            var value = profile.Get(factor.Factor);
            var next = NextValue(factor.Factor, rows.Where(r => r.Factor == factor.Factor).ToList(), value, factor.Points);
            if (next is null)
            {
                continue;
            }

            var after = evaluate(ProfileAdjust.Set(profile, factor.Factor, next));
            var gained = impact(after);
            if (gained <= 0)
            {
                continue;
            }

            var hint = FactorHints.For(factor.Factor);
            var requirement = RelatedRequirement(pathway, factor.Factor);
            var months = FactorHints.IsYearCount(factor.Factor) && next is ProfileValue.Number target
                ? (int)Math.Ceiling((target.Value - (value is ProfileValue.Number n ? n.Value : 0)) * 12)
                : requirement?.FixMonths ?? hint.FixMonths;

            yield return new Gap(
                null,
                factor.Factor,
                hint.Label,
                value is null ? RequirementChecker.NotProvided : ValueText.Describe(value, hint.Unit),
                ValueText.Describe(next, hint.Unit),
                requirement?.Fixability ?? hint.Fixability,
                requirement?.FixHint ?? hint.FixHint,
                months,
                gained,
                after.Grade.Grade);
        }
    }

    /// <summary>The lowest bracket or level above the current one that scores more points.</summary>
    private static ProfileValue? NextValue(string factor, List<PointsFactorRule> rows, ProfileValue? value, int currentPoints)
    {
        static int Capped(PointsFactorRule r) => r.MaxPointsForFactor is { } cap ? Math.Min(r.Points, cap) : r.Points;

        if (rows.All(r => r.MatchValue is null))
        {
            decimal? current = value switch
            {
                null or ProfileValue.Below => null,
                ProfileValue.Number n => FactorHints.IsYearCount(factor) ? Math.Floor(n.Value) : n.Value,
                _ => decimal.MaxValue, // money, text, levels: no numeric step to suggest
            };
            var row = rows
                .Where(r => r.Min is { } min && (current is null || min > current) && Capped(r) > currentPoints)
                .OrderBy(r => r.Min)
                .FirstOrDefault();
            return row is null ? null : new ProfileValue.Number(row.Min!.Value);
        }

        var scale = value is ProfileValue.Ordinal o ? o.Scale
            : value is null ? rows.Select(r => r.MatchValue).OfType<string>().Select(OrdinalScale.FindByLabel).FirstOrDefault(s => s is not null)
            : null;
        if (scale is null)
        {
            return null; // text and yes/no factors (marital status, relatives) are not suggested
        }

        var currentRank = value is ProfileValue.Ordinal ordinal ? ordinal.Rank : -1;
        var best = rows
            .Select(r => (Row: r, Rank: r.MatchValue is { } m ? scale.RankOf(m) : null))
            .Where(x => x.Rank > currentRank && Capped(x.Row) > currentPoints)
            .OrderBy(x => x.Rank)
            .FirstOrDefault();
        return best.Rank is { } rank ? new ProfileValue.Ordinal(scale, rank) : null;
    }

    /// <summary>
    /// A "reach this level" requirement (<c>gte</c>/<c>gt</c>) on the same field, or on the overall level for a
    /// per-skill key: its fix also raises the factor. Other requirements on the field (e.g. "has a credential")
    /// say nothing about how to improve it.
    /// </summary>
    private static RequirementRule? RelatedRequirement(PathwayRule pathway, string field)
    {
        var parent = field.LastIndexOf('.') is > 0 and var dot ? field[..dot] : null;
        var raising = pathway.Requirements
            .Where(r => r.Operator is RequirementOperator.Gte or RequirementOperator.Gt)
            .ToList();
        return raising.FirstOrDefault(r => r.Field == field)
            ?? raising.FirstOrDefault(r => r.Field == parent && parent.Contains(".level.", StringComparison.Ordinal));
    }

    /// <summary>A value that meets the requirement, typed like the profile value; null when none can be derived.</summary>
    private static ProfileValue? SatisfyingValue(RequirementRule rule, ProfileValue? current, ExchangeRates rates)
    {
        if (rule.Operator == RequirementOperator.True)
        {
            return new ProfileValue.Flag(true);
        }

        if (rule.Value is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(rule.Value);
        var json = document.RootElement;
        var step = rule.Operator switch
        {
            RequirementOperator.Eq or RequirementOperator.Gte or RequirementOperator.Lte => 0,
            RequirementOperator.Gt => 1,
            RequirementOperator.Lt => -1,
            RequirementOperator.In when json.ValueKind == JsonValueKind.Array && json.GetArrayLength() > 0 => 0,
            _ => (int?)null,
        };
        if (step is null)
        {
            return null;
        }

        if (rule.Operator == RequirementOperator.In)
        {
            json = json[0];
        }

        return json.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False => new ProfileValue.Flag(json.GetBoolean()),
            JsonValueKind.Number => NumberValue(json.GetDecimal() + step.Value, rule.Unit, current, rates),
            JsonValueKind.String => LevelOrText(json.GetString()!, step.Value, rule.Unit, current),
            _ => null,
        };
    }

    private static ProfileValue NumberValue(decimal amount, string? unit, ProfileValue? current, ExchangeRates rates)
    {
        var isCurrency = unit is not null && (unit == ExchangeRates.Usd || rates.PerUsd.ContainsKey(unit));
        return current switch
        {
            ProfileValue.Money money => new ProfileValue.Money(amount, unit ?? money.Currency),
            null when isCurrency => new ProfileValue.Money(amount, unit!),
            _ => new ProfileValue.Number(amount),
        };
    }

    private static ProfileValue? LevelOrText(string label, int step, string? unit, ProfileValue? current)
    {
        var scale = current switch
        {
            ProfileValue.Ordinal o => o.Scale,
            ProfileValue.Below b => OrdinalScale.Find(b.Scale),
            _ => (unit is null ? null : OrdinalScale.Find(unit)) ?? OrdinalScale.FindByLabel(label),
        };
        if (scale?.RankOf(label) is not { } rank)
        {
            return current is ProfileValue.Text or null ? new ProfileValue.Text(label) : null;
        }

        rank += step;
        return rank >= 0 && rank < scale.Labels.Count ? new ProfileValue.Ordinal(scale, rank) : null;
    }
}
