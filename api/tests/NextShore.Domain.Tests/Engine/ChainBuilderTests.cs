using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using static NextShore.Domain.Tests.Engine.TestRules;
using Op = NextShore.Domain.Enums.RequirementOperator;

namespace NextShore.Domain.Tests.Engine;

public class ChainBuilderTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal>());

    private static NormalizedProfile Profile(params (string Key, ProfileValue Value)[] values) =>
        new(values.ToDictionary(v => v.Key, v => v.Value));

    private static ProfileValue Num(decimal value) => new ProfileValue.Number(value);

    private static ProfileValue Bachelors => new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 2);

    private static IReadOnlyList<PathwayChain> Chains(NormalizedProfile profile, params PathwayRule[] pathways) =>
        ChainBuilder.Build(new RuleSnapshot(1, [], pathways, []), profile, Rates, GraderOptions.Default);

    private static PathwayRule Study(string slug, string country, string? next, int? months = 24,
        params RequirementRule[] requirements) =>
        Pathway(slug: slug, type: PathwayType.Study, country: country, next: next, durationMonths: months, requirements: requirements);

    private static PathwayRule PostStudy(string slug, string country, string? next, int? months = 36,
        params RequirementRule[] requirements) =>
        Pathway(slug: slug, type: PathwayType.PostStudyWork, country: country, next: next, durationMonths: months, requirements: requirements);

    [Fact]
    public void A_chain_starts_at_each_study_pathway_and_follows_next()
    {
        var chains = Chains(Profile(),
            Study("ca-study", "CA", "ca-pgwp"),
            PostStudy("ca-pgwp", "CA", "ca-pr"),
            Pathway(slug: "ca-pr", country: "CA"),
            Study("de-study", "DE", "de-post"),
            PostStudy("de-post", "DE", null),
            Pathway(slug: "de-work", type: PathwayType.Work, country: "DE", next: "de-post"));

        Assert.Equal(
            [("CA", new[] { "ca-study", "ca-pgwp", "ca-pr" }), ("DE", new[] { "de-study", "de-post" })],
            chains.Select(c => (c.CountryCode, c.Steps.Select(s => s.PathwaySlug).ToArray())));
    }

    [Fact]
    public void First_step_uses_the_real_profile_and_later_steps_are_projected()
    {
        var chain = Assert.Single(Chains(Profile(), Study("ca-study", "CA", "ca-pgwp"), PostStudy("ca-pgwp", "CA", null)));

        Assert.Equal([false, true], chain.Steps.Select(s => s.IsProjected));
        Assert.Equal([PathwayType.Study, PathwayType.PostStudyWork], chain.Steps.Select(s => s.Type));
    }

    [Fact]
    public void Steps_carry_their_grade_and_readiness()
    {
        var chain = Assert.Single(Chains(Profile(("english.level.CLB", Num(6))),
            Study("ca-study", "CA", "ca-pgwp"),
            PostStudy("ca-pgwp", "CA", null, requirements: Requirement("english.level.CLB", Op.Gte, "7", "CLB"))));

        Assert.Equal((Grade.Eligible, 100), (chain.Steps[0].Grade, chain.Steps[0].ReadinessScore));
        Assert.Equal((Grade.NearEligible, 40), (chain.Steps[1].Grade, chain.Steps[1].ReadinessScore));
    }

    [Fact]
    public void Study_projects_the_next_education_level()
    {
        var chain = Assert.Single(Chains(Profile(("education.level", Bachelors)),
            Study("ca-study", "CA", "ca-pr"),
            Pathway(slug: "ca-pr", country: "CA", requirements: [Requirement("education.level", Op.In, """["masters"]""")])));

        Assert.Equal(Grade.Eligible, chain.Steps[1].Grade);
    }

    [Fact]
    public void Study_projects_a_degree_from_that_country()
    {
        var chain = Assert.Single(Chains(Profile(("education.level", Bachelors)),
            Study("de-study", "DE", "de-post"),
            PostStudy("de-post", "DE", null, requirements: Requirement("degree.recognized_DE", Op.True, null))));

        Assert.Equal(Grade.Eligible, chain.Steps[1].Grade);
    }

    [Fact]
    public void Post_study_work_adds_work_experience_in_the_country()
    {
        var chain = Assert.Single(Chains(
            Profile(("work.years_skilled", Num(0.5m)), ("work.years_in_country.DE", Num(1))),
            Study("ca-study", "CA", "ca-pgwp"),
            PostStudy("ca-pgwp", "CA", "ca-pr", months: 36),
            Pathway(slug: "ca-pr", country: "CA", requirements:
            [
                Requirement("work.years_in_country.CA", Op.Gte, "3", "years"),
                Requirement("work.years_skilled", Op.Gte, "3.5", "years"),
                Requirement("work.years_in_country.DE", Op.Eq, "1", "years"),
            ])));

        Assert.Equal(Grade.Eligible, chain.Steps[2].Grade);
    }

    [Fact]
    public void Age_advances_by_the_whole_years_already_spent()
    {
        var chain = Assert.Single(Chains(Profile(("age", Num(34))),
            Study("au-study", "AU", "au-485", months: 23),
            PostStudy("au-485", "AU", "au-pr", months: 24,
                requirements: Requirement("age", Op.Lt, "36", "years", Fixability.Impossible)),
            Pathway(slug: "au-pr", country: "AU", requirements: [Requirement("age", Op.Lt, "37", "years", Fixability.Impossible)])));

        Assert.Equal(Grade.Eligible, chain.Steps[1].Grade);    // 34 + 1 (23 months) = 35
        Assert.Equal(Grade.NotEligible, chain.Steps[2].Grade); // 34 + 3 (47 months) = 37
    }

    [Fact]
    public void Total_is_the_sum_of_step_durations()
    {
        var chain = Assert.Single(Chains(Profile(),
            Study("ca-study", "CA", "ca-pgwp", months: 24),
            PostStudy("ca-pgwp", "CA", "ca-pr", months: 36),
            Pathway(slug: "ca-pr", country: "CA", durationMonths: 6)));

        Assert.Equal(66, chain.TotalMonths);
        Assert.Equal([24, 36, 6], chain.Steps.Select(s => s.DurationMonths));
    }

    [Fact]
    public void Unknown_duration_leaves_the_total_unknown_and_skips_time_projections()
    {
        var chain = Assert.Single(Chains(Profile(("age", Num(35)), ("education.level", Bachelors)),
            Study("au-study", "AU", "au-485", months: null),
            PostStudy("au-485", "AU", null, requirements:
            [
                Requirement("age", Op.Lt, "36", "years"),
                Requirement("education.level", Op.In, """["masters"]"""),
            ])));

        Assert.Null(chain.TotalMonths);
        Assert.Equal(Grade.Eligible, chain.Steps[1].Grade); // age not advanced; education still projected
    }

    [Fact]
    public void A_cycle_in_next_pathways_stops_the_chain()
    {
        var chain = Assert.Single(Chains(Profile(),
            Study("xx-study", "XX", "xx-post"),
            PostStudy("xx-post", "XX", "xx-study")));

        Assert.Equal(["xx-study", "xx-post"], chain.Steps.Select(s => s.PathwaySlug));
    }

    [Fact]
    public void Next_pathway_missing_from_the_snapshot_ends_the_chain()
    {
        var chain = Assert.Single(Chains(Profile(), Study("xx-study", "XX", "xx-unpublished")));

        Assert.Equal(["xx-study"], chain.Steps.Select(s => s.PathwaySlug));
    }
}
