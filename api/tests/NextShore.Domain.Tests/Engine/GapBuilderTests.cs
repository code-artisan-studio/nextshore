using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using static NextShore.Domain.Tests.Engine.TestRules;
using Op = NextShore.Domain.Enums.RequirementOperator;

namespace NextShore.Domain.Tests.Engine;

public class GapBuilderTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal> { ["CAD"] = 1.37m });

    private static NormalizedProfile Profile(params (string Key, ProfileValue Value)[] values) =>
        new(values.ToDictionary(v => v.Key, v => v.Value));

    private static ProfileValue Num(decimal value) => new ProfileValue.Number(value);

    private static IReadOnlyList<Gap> Gaps(NextShore.Domain.Rules.PathwayRule pathway, NormalizedProfile profile) =>
        GapBuilder.Build(pathway, profile, Rates, GraderOptions.Default);

    [Fact]
    public void Eligible_profile_has_no_gaps()
    {
        var pathway = Pathway(requirements: [Requirement("work.years_skilled", Op.Gte, "1", "years")]);

        Assert.Empty(Gaps(pathway, Profile(("work.years_skilled", Num(3)))));
    }

    [Fact]
    public void Each_unmet_mandatory_requirement_is_a_gap_and_advisory_ones_are_not()
    {
        var pathway = Pathway(requirements:
        [
            Requirement("english.level.CLB", Op.Gte, "7", "CLB", Fixability.Easy, label: "English"),
            Requirement("work.years_in_country.CA", Op.Gte, "1", "years", mandatory: false, label: "Advisory"),
        ]);

        var gap = Assert.Single(Gaps(pathway, Profile(("english.level.CLB", Num(6)), ("work.years_in_country.CA", Num(0)))));

        Assert.Equal(pathway.Requirements[0].Id, gap.RequirementId);
        Assert.Equal("english.level.CLB", gap.Field);
        Assert.Equal("English", gap.Label);
        Assert.Equal("CLB 6", gap.Current);
        Assert.Equal("CLB 7", gap.Target);
        Assert.Equal(Fixability.Easy, gap.Fixability);
        Assert.Equal("Fix it", gap.FixHint);
        Assert.Equal(3, gap.FixMonths);
    }

    [Fact]
    public void Requirement_gap_reports_the_grade_after_fixing_it()
    {
        var pathway = Pathway(requirements: [Requirement("english.level.CLB", Op.Gte, "7", "CLB")]);

        var gap = Assert.Single(Gaps(pathway, Profile(("english.level.CLB", Num(6)))));

        Assert.Equal(Grade.Eligible, gap.UnlocksGrade);
        Assert.Equal(0, gap.ImpactPoints);
    }

    [Fact]
    public void Requirement_gap_counts_the_points_the_fix_adds()
    {
        var pathway = Pathway(
            [Factor("english.level.CLB", 6, 6, points: 8), Factor("english.level.CLB", 7, 7, points: 16)],
            passMark: 10,
            requirements: [Requirement("english.level.CLB", Op.Gte, "7", "CLB")]);

        var gap = Assert.Single(Gaps(pathway, Profile(("english.level.CLB", Num(6)))), g => g.RequirementId is not null);

        Assert.Equal(8, gap.ImpactPoints);
        Assert.Equal(Grade.Eligible, gap.UnlocksGrade);
    }

    [Fact]
    public void Money_requirement_is_fixed_in_the_rule_currency()
    {
        var pathway = Pathway(requirements: [Requirement("funds.amount_usd", Op.Gte, "23448", "CAD")]);

        var gap = Assert.Single(Gaps(pathway, Profile(("funds.amount_usd", new ProfileValue.Money(15000, "USD")))));

        Assert.Equal("USD 15,000 (≈ CAD 20,550)", gap.Current);
        Assert.Equal("CAD 23,448", gap.Target);
        Assert.Equal(Grade.Eligible, gap.UnlocksGrade);
    }

    [Fact]
    public void Missing_answers_are_easy_gaps_that_can_still_unlock_a_grade()
    {
        var pathway = Pathway(requirements:
        [
            Requirement("job_offer.DE", Op.True, null, fixability: Fixability.Medium),
            Requirement("education.level", Op.In, """["bachelors","masters"]""", fixability: Fixability.Hard),
        ]);

        var gaps = Gaps(pathway, Profile(("job_offer.DE", new ProfileValue.Flag(false))));

        var jobOffer = Assert.Single(gaps, g => g.Field == "job_offer.DE");
        var education = Assert.Single(gaps, g => g.Field == "education.level");
        Assert.Equal(Grade.NearEligible, jobOffer.UnlocksGrade); // education still missing
        Assert.Equal(Fixability.Easy, education.Fixability);
        Assert.Equal("Add this to your profile", education.FixHint);
        Assert.Equal(Grade.NearEligible, education.UnlocksGrade);
    }

    [Fact]
    public void Impossible_gaps_are_not_simulated_and_sort_last()
    {
        var pathway = Pathway(requirements:
        [
            Requirement("age", Op.Lt, "45", "years", Fixability.Impossible, label: "Age"),
            Requirement("education.level", Op.In, """["masters"]""", fixability: Fixability.Hard, label: "Degree"),
            Requirement("english.level.CLB", Op.Gte, "7", "CLB", Fixability.Easy, label: "English"),
        ]);
        var profile = Profile(
            ("age", Num(50)),
            ("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 2)),
            ("english.level.CLB", Num(6)));

        var gaps = Gaps(pathway, profile);

        Assert.Equal(["English", "Degree", "Age"], gaps.Select(g => g.Label));
        Assert.Null(gaps[2].UnlocksGrade);
        Assert.Equal(0, gaps[2].ImpactPoints);
    }

    [Fact]
    public void Points_shortfall_reports_the_three_best_improvements_sorted_by_impact_per_month()
    {
        var pathway = Pathway(
        [
            Factor("age", 20, 29, points: 30),
            Factor("work.years_in_country.CA", 1, 1, points: 35),
            Factor("work.years_in_country.CA", 2, 2, points: 46),
            Factor("education.level", match: "bachelors", points: 10),
            Factor("education.level", match: "masters", points: 25),
            Factor("english.level.IELTS", 7, 7, points: 10),
            Factor("english.level.IELTS", 8, null, points: 20),
            Factor("french.level.CLB", 5, 6, points: 4),
        ],
        cutoff: 100,
        requirements: [Requirement("english.level.IELTS", Op.Gte, "6", "IELTS", Fixability.Easy)]);
        var profile = Profile(
            ("age", Num(31)),
            ("work.years_in_country.CA", Num(1)),
            ("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 2)),
            ("english.level.IELTS", Num(7)));

        var gaps = Gaps(pathway, profile);

        // impact/months: English 10/3, Canadian work 11/12, education 15/24; French (+4) is 4th best and dropped.
        Assert.Equal(["english.level.IELTS", "work.years_in_country.CA", "education.level"], gaps.Select(g => g.Field));
        Assert.Equal([10, 11, 15], gaps.Select(g => g.ImpactPoints));
        Assert.Equal(["IELTS 7", "1 year", "bachelors"], gaps.Select(g => g.Current));
        Assert.Equal(["IELTS 8", "2 years", "masters"], gaps.Select(g => g.Target));
        Assert.Equal([3, 12, 24], gaps.Select(g => g.FixMonths));
        Assert.All(gaps, g => Assert.Null(g.RequirementId));
        Assert.All(gaps, g => Assert.Equal(Grade.NotEligible, g.UnlocksGrade));
    }

    [Fact]
    public void Improvement_targets_the_next_bracket_and_the_months_to_reach_it()
    {
        var pathway = Pathway(
            [
                Factor("work.years_in_country.CA", 1, 1, points: 35),
                Factor("work.years_in_country.CA", 2, 2, points: 46),
                Factor("work.years_in_country.CA", 3, 3, points: 56),
            ],
            cutoff: 100);

        var gap = Assert.Single(Gaps(pathway, Profile(("work.years_in_country.CA", Num(1.5m)))));

        Assert.Equal("1.5 years", gap.Current);
        Assert.Equal("2 years", gap.Target);
        Assert.Equal(6, gap.FixMonths);
        Assert.Equal(11, gap.ImpactPoints);
    }

    [Fact]
    public void Age_is_never_suggested()
    {
        var pathway = Pathway([Factor("age", 18, 24, points: 25), Factor("age", 25, 32, points: 30)], cutoff: 100);

        var gap = Assert.Single(Gaps(pathway, Profile(("age", Num(40)))));

        Assert.Equal("points", gap.Field); // only the shortfall
    }

    [Fact]
    public void Missing_factor_suggests_its_lowest_scoring_level()
    {
        var pathway = Pathway(
            [Factor("german.level.CEFR", match: "A2", points: 1), Factor("german.level.CEFR", match: "B1", points: 2)],
            passMark: 6);

        var gap = Assert.Single(Gaps(pathway, Profile()));

        Assert.Equal("german.level.CEFR", gap.Field);
        Assert.Equal("Not provided", gap.Current);
        Assert.Equal("CEFR A2", gap.Target);
        Assert.Equal(1, gap.ImpactPoints);
    }

    [Fact]
    public void Shortfall_with_no_possible_improvement_is_a_single_points_gap()
    {
        var pathway = Pathway([Factor("work.years_skilled", 5, null, points: 50)], cutoff: 100);

        var gap = Assert.Single(Gaps(pathway, Profile(("work.years_skilled", Num(6)))));

        Assert.Equal(new Gap(null, "points", "Points", "50 points", "100 points", Fixability.Hard,
            "No single change closes the points gap", null, 0, null), gap);
    }

    [Fact]
    public void Language_skill_and_overall_level_move_together()
    {
        // Overall CLB is the lowest skill (writing 6). Raising writing to 7 also meets the overall CLB 7 requirement.
        var pathway = Pathway(
            [
                Factor("work.years_skilled", 5, null, points: 95),
                Factor("english.level.CLB.writing", 6, 6, points: 8),
                Factor("english.level.CLB.writing", 7, 7, points: 16),
            ],
            cutoff: 110,
            requirements: [Requirement("english.level.CLB", Op.Gte, "7", "CLB", Fixability.Easy, label: "English")]);
        var profile = Profile(
            ("work.years_skilled", Num(5)),
            ("english.level.CLB", Num(6)),
            ("english.level.CLB.listening", Num(9)),
            ("english.level.CLB.reading", Num(7)),
            ("english.level.CLB.writing", Num(6)),
            ("english.level.CLB.speaking", Num(9)));

        var gaps = Gaps(pathway, profile);

        var requirement = Assert.Single(gaps, g => g.RequirementId is not null);
        var writing = Assert.Single(gaps, g => g.Field == "english.level.CLB.writing");
        Assert.Equal((8, Grade.Eligible), (requirement.ImpactPoints, requirement.UnlocksGrade)); // raises writing
        Assert.Equal((8, Grade.Eligible), (writing.ImpactPoints, writing.UnlocksGrade));         // raises overall
        Assert.Equal("English writing", writing.Label);
        Assert.Equal(("CLB 6", "CLB 7"), (writing.Current, writing.Target));
        Assert.Equal(3, writing.FixMonths); // from the English requirement's fix
    }
}
