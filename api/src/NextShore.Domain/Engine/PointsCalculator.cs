using System.Globalization;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

public enum PointsTargetKind { Cutoff, PassMark }

public sealed record FactorPoints(string Factor, string Current, int Points);

public sealed record PointsBreakdown(
    PointsVariant Variant,
    int Total,
    int? Target,
    PointsTargetKind? TargetKind,
    IReadOnlyList<FactorPoints> Breakdown);

/// <summary>Scores a points-based pathway (VISA-RULES-SPEC.md §1.3).</summary>
public static class PointsCalculator
{
    /// <summary>Null for pathways that are not points-based.</summary>
    public static PointsBreakdown? Calculate(PathwayRule pathway, NormalizedProfile profile)
    {
        if (!pathway.IsPointsBased)
        {
            return null;
        }

        var variant = ChooseVariant(pathway, profile);
        var rows = pathway.PointsFactors.Where(r => r.Variant == variant).ToList();

        var breakdown = rows
            .Select(r => r.Factor)
            .Distinct()
            .Select(factor => Score(factor, rows.Where(r => r.Factor == factor), profile.Get(factor)))
            .ToList();

        var (target, kind) = pathway.Cutoff is { } cutoff ? (cutoff, PointsTargetKind.Cutoff)
            : pathway.PassMark is { } passMark ? (passMark, PointsTargetKind.PassMark)
            : ((int?)null, (PointsTargetKind?)null);

        return new PointsBreakdown(variant, breakdown.Sum(f => f.Points), target, kind, breakdown);
    }

    /// <summary><c>with_spouse</c> if married and the pathway has such rows, else <c>single</c>, else <c>default</c>.</summary>
    private static PointsVariant ChooseVariant(PathwayRule pathway, NormalizedProfile profile)
    {
        bool Has(PointsVariant v) => pathway.PointsFactors.Any(r => r.Variant == v);
        var married = profile.Get("marital_status") is ProfileValue.Text { Value: var status }
            && string.Equals(status, "married", StringComparison.OrdinalIgnoreCase);

        return married && Has(PointsVariant.WithSpouse) ? PointsVariant.WithSpouse
            : Has(PointsVariant.Single) ? PointsVariant.Single
            : PointsVariant.Default;
    }

    /// <summary>The best matching row for the factor, capped; 0 when nothing matches or the value is missing.</summary>
    private static FactorPoints Score(string factor, IEnumerable<PointsFactorRule> rows, ProfileValue? value)
    {
        if (value is null)
        {
            return new FactorPoints(factor, RequirementChecker.NotProvided, 0);
        }

        var points = rows
            .Where(row => Matches(row, factor, value))
            .Select(row => row.MaxPointsForFactor is { } cap ? Math.Min(row.Points, cap) : row.Points)
            .DefaultIfEmpty(0)
            .Max();

        return new FactorPoints(factor, ValueText.Describe(value, FactorHints.For(factor).Unit), points);
    }

    private static bool Matches(PointsFactorRule row, string factor, ProfileValue value)
    {
        if (row.MatchValue is { } match)
        {
            return value switch
            {
                ProfileValue.Text t => string.Equals(t.Value, match, StringComparison.OrdinalIgnoreCase),
                ProfileValue.Ordinal o => string.Equals(o.Label, match, StringComparison.OrdinalIgnoreCase),
                ProfileValue.Flag f => bool.TryParse(match, out var expected) && f.Value == expected,
                ProfileValue.Number n => decimal.TryParse(match, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && n.Value == d,
                _ => false,
            };
        }

        if (row.Min is null && row.Max is null)
        {
            return true; // a row without a bracket or match applies to any provided value
        }

        var number = value switch
        {
            ProfileValue.Number n => FactorHints.IsYearCount(factor) ? Math.Floor(n.Value) : n.Value,
            ProfileValue.Money m => m.Amount,
            ProfileValue.Below => (decimal?)null,
            _ => throw new InvalidOperationException(
                $"Points factor '{factor}' ({row.Variant}) has a numeric bracket but the value is {value.GetType().Name.ToLowerInvariant()}; use matchValue."),
        };

        return number is { } v && (row.Min is null || v >= row.Min) && (row.Max is null || v <= row.Max);
    }
}
