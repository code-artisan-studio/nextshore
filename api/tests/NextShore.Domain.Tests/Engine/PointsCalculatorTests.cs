using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using static NextShore.Domain.Tests.Engine.TestRules;
using V = NextShore.Domain.Enums.PointsVariant;

namespace NextShore.Domain.Tests.Engine;

public class PointsCalculatorTests
{
    private static NormalizedProfile Profile(params (string Key, ProfileValue Value)[] values) =>
        new(values.ToDictionary(v => v.Key, v => v.Value));

    private static ProfileValue Num(decimal value) => new ProfileValue.Number(value);

    private static int PointsFor(string factor, ProfileValue value, params PointsFactorRule[] rows) =>
        PointsCalculator.Calculate(Pathway(rows), Profile((factor, value)))!.Total;

    [Fact]
    public void Pathway_without_points_has_no_breakdown()
    {
        Assert.Null(PointsCalculator.Calculate(Pathway(), Profile(("age", Num(30)))));
    }

    [Fact]
    public void Points_are_summed_across_factors()
    {
        var pathway = Pathway(
        [
            Factor("age", 20, 29, points: 110),
            Factor("age", 30, 30, points: 105),
            Factor("education.level", match: "masters", points: 135),
            Factor("work.years_in_country.CA", 1, 1, points: 40),
        ]);
        var profile = Profile(
            ("age", Num(30)),
            ("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 3)),
            ("work.years_in_country.CA", Num(1)));

        var points = PointsCalculator.Calculate(pathway, profile)!;

        Assert.Equal(105 + 135 + 40, points.Total);
    }

    [Theory]
    [InlineData("married", true, true, V.WithSpouse)]
    [InlineData("single", true, true, V.Single)]
    [InlineData("married", false, true, V.Single)]  // no with-spouse rows → single
    [InlineData("married", false, false, V.Default)]
    [InlineData(null, true, true, V.Single)]
    public void Variant_follows_marital_status_and_available_rows(
        string? maritalStatus, bool hasWithSpouse, bool hasSingle, V expected)
    {
        var rows = new List<PointsFactorRule> { Factor("age", 0, null, points: 1, variant: V.Default) };
        if (hasWithSpouse) rows.Add(Factor("age", 0, null, points: 2, variant: V.WithSpouse));
        if (hasSingle) rows.Add(Factor("age", 0, null, points: 3, variant: V.Single));
        var values = new List<(string, ProfileValue)> { ("age", Num(30)) };
        if (maritalStatus is not null) values.Add(("marital_status", new ProfileValue.Text(maritalStatus)));

        var points = PointsCalculator.Calculate(Pathway(rows), Profile([.. values]))!;

        Assert.Equal(expected, points.Variant);
        Assert.Equal(expected switch { V.WithSpouse => 2, V.Single => 3, _ => 1 }, points.Total);
    }

    [Theory]
    [InlineData(17, 0)]
    [InlineData(18, 25)] // min inclusive
    [InlineData(24, 25)] // max inclusive
    [InlineData(25, 30)]
    [InlineData(44, 15)]
    [InlineData(45, 0)]  // no bracket
    public void Brackets_are_inclusive_at_both_ends(int age, int expected)
    {
        var points = PointsFor("age", Num(age),
            Factor("age", 18, 24, points: 25),
            Factor("age", 25, 32, points: 30),
            Factor("age", 40, 44, points: 15));

        Assert.Equal(expected, points);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(8, 15)]
    [InlineData(30, 15)]
    public void Open_bracket_has_no_upper_bound(decimal years, int expected)
    {
        Assert.Equal(expected, PointsFor("work.years_skilled", Num(years), Factor("work.years_skilled", 8, null, points: 15)));
    }

    [Theory]
    [InlineData("work.years_in_country.CA", 1.9, 40)]  // floored to 1
    [InlineData("work.years_in_country.CA", 0.99, 0)]  // floored to 0
    [InlineData("work.years_skilled", 1.5, 40)]
    public void Year_factors_count_whole_years(string factor, decimal years, int expected)
    {
        Assert.Equal(expected, PointsFor(factor, Num(years), Factor(factor, 1, 1, points: 40)));
    }

    [Fact]
    public void Other_numeric_factors_are_not_rounded()
    {
        Assert.Equal(10, PointsFor("english.score.listening", Num(7.5m), Factor("english.score.listening", 7.5m, null, points: 10)));
        Assert.Equal(0, PointsFor("english.score.listening", Num(7.4m), Factor("english.score.listening", 7.5m, null, points: 10)));
    }

    [Fact]
    public void Categorical_rows_match_ordinals_text_and_flags()
    {
        Assert.Equal(20, PointsFor("german.level.CEFR", new ProfileValue.Ordinal(OrdinalScale.Cefr, 3),
            Factor("german.level.CEFR", match: "B1", points: 10),
            Factor("german.level.CEFR", match: "B2", points: 20)));
        Assert.Equal(10, PointsFor("marital_status", new ProfileValue.Text("single"),
            Factor("marital_status", match: "Single", points: 10)));
        Assert.Equal(15, PointsFor("relative_in.CA", new ProfileValue.Flag(true),
            Factor("relative_in.CA", match: "true", points: 15)));
        Assert.Equal(0, PointsFor("relative_in.CA", new ProfileValue.Flag(false),
            Factor("relative_in.CA", match: "true", points: 15)));
    }

    [Theory]
    [InlineData(6, 4)]
    [InlineData(8, 12)]
    [InlineData(9, 22)] // 24 capped at 22
    public void Factor_cap_limits_points(int clb, int expected)
    {
        Assert.Equal(expected, PointsFor("french.level.CLB", Num(clb),
            Factor("french.level.CLB", 5, 6, points: 4, cap: 22),
            Factor("french.level.CLB", 7, 8, points: 12, cap: 22),
            Factor("french.level.CLB", 9, null, points: 24, cap: 22)));
    }

    [Fact]
    public void Highest_matching_row_wins_when_brackets_overlap()
    {
        Assert.Equal(2, PointsFor("age", Num(35), Factor("age", null, 35, points: 2), Factor("age", 35, 40, points: 1)));
    }

    [Fact]
    public void Missing_value_scores_zero_and_is_shown_as_not_provided()
    {
        var pathway = Pathway([Factor("age", 18, 24, points: 25), Factor("french.level.CLB", 5, null, points: 4)]);

        var points = PointsCalculator.Calculate(pathway, Profile(("age", Num(20))))!;

        Assert.Equal(25, points.Total);
        Assert.Equal(new FactorPoints("french.level.CLB", "Not provided", 0), points.Breakdown[1]);
    }

    [Fact]
    public void Below_the_scale_scores_zero()
    {
        Assert.Equal(0, PointsFor("english.level.CLB.listening", new ProfileValue.Below("CLB", "4"),
            Factor("english.level.CLB.listening", 4, 5, points: 6)));
    }

    [Fact]
    public void Breakdown_lists_each_factor_once_in_rule_order_with_its_value()
    {
        var pathway = Pathway(
        [
            Factor("age", 18, 24, points: 25),
            Factor("education.level", match: "masters", points: 15),
            Factor("age", 25, 32, points: 30),
        ]);
        var profile = Profile(
            ("age", Num(28)),
            ("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 2)));

        var points = PointsCalculator.Calculate(pathway, profile)!;

        Assert.Equal(
            [new FactorPoints("age", "28", 30), new FactorPoints("education.level", "bachelors", 0)],
            points.Breakdown);
    }

    [Theory]
    [InlineData(65, 518, 518, PointsTargetKind.Cutoff)]
    [InlineData(65, null, 65, PointsTargetKind.PassMark)]
    [InlineData(null, null, null, null)]
    public void Target_prefers_the_cutoff_over_the_pass_mark(
        int? passMark, int? cutoff, int? expectedTarget, PointsTargetKind? expectedKind)
    {
        var points = PointsCalculator.Calculate(
            Pathway([Factor("age", 0, null, points: 1)], passMark, cutoff), Profile(("age", Num(30))))!;

        Assert.Equal(expectedTarget, points.Target);
        Assert.Equal(expectedKind, points.TargetKind);
    }

    [Fact]
    public void A_bracket_on_a_level_value_is_a_rule_error()
    {
        Assert.Throws<InvalidOperationException>(() => PointsFor("german.level.CEFR",
            new ProfileValue.Ordinal(OrdinalScale.Cefr, 2), Factor("german.level.CEFR", 1, 3, points: 1)));
    }
}
