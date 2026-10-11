using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using static NextShore.Domain.Tests.Engine.TestRules;
using Op = NextShore.Domain.Enums.RequirementOperator;

namespace NextShore.Domain.Tests.Engine;

public class EligibilityEngineTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal>());

    private static NormalizedProfile Developer(params (string Key, ProfileValue Value)[] extra) =>
        new(new Dictionary<string, ProfileValue>(extra.Select(e => KeyValuePair.Create(e.Key, e.Value)))
        {
            ["audience"] = new ProfileValue.Text("developer"),
            ["work.years_skilled"] = new ProfileValue.Number(5),
        });

    private static NormalizedProfile Student() => new(new Dictionary<string, ProfileValue>
    {
        ["audience"] = new ProfileValue.Text("student"),
        ["work.years_skilled"] = new ProfileValue.Number(0),
    });

    private static EngineResult Evaluate(NormalizedProfile profile, params PathwayRule[] pathways) =>
        EligibilityEngine.Evaluate(profile, new RuleSnapshot(7, [], pathways, []), Rates, EngineOptions.Default, Today);

    private static readonly Audience[] StudentOnly = [Audience.Student];
    private static readonly Audience[] DeveloperOnly = [Audience.Developer];

    [Fact]
    public void Developers_only_see_developer_pathways_and_get_no_chains()
    {
        var result = Evaluate(Developer(),
            Pathway(slug: "ca-study", type: PathwayType.Study, next: "ca-pr", audience: StudentOnly),
            Pathway(slug: "ca-pr", audience: [Audience.Developer, Audience.Student]));

        Assert.Equal(["ca-pr"], result.Results.Select(r => r.PathwaySlug));
        Assert.Empty(result.Chains);
    }

    [Fact]
    public void Students_also_see_work_and_pr_pathways_at_the_end_of_their_chain()
    {
        var result = Evaluate(Student(),
            Pathway(slug: "de-study", type: PathwayType.Study, next: "de-blue-card", audience: StudentOnly),
            Pathway(slug: "de-blue-card", type: PathwayType.Work, audience: DeveloperOnly),
            Pathway(slug: "de-other-work", type: PathwayType.Work, audience: DeveloperOnly));

        Assert.Equal(["de-blue-card", "de-study"], result.Results.Select(r => r.PathwaySlug).Order());
        Assert.Equal(["de-study", "de-blue-card"], Assert.Single(result.Chains).Steps.Select(s => s.PathwaySlug));
    }

    [Fact]
    public void Results_are_ranked_by_readiness_then_slug()
    {
        var result = Evaluate(Developer(),
            Pathway(slug: "b-half", requirements: [Requirement("work.years_skilled", Op.Gte, "1"), Requirement("missing", Op.Exists, null)]),
            Pathway(slug: "c-full", requirements: [Requirement("work.years_skilled", Op.Gte, "1")]),
            Pathway(slug: "a-full"));

        Assert.Equal(["a-full", "c-full", "b-half"], result.Results.Select(r => r.PathwaySlug));
        Assert.Equal([100, 100, 70], result.Results.Select(r => r.ReadinessScore));
    }

    [Fact]
    public void Each_result_carries_requirements_points_gaps_and_its_chain()
    {
        var result = Evaluate(Student(),
            Pathway(slug: "ca-study", type: PathwayType.Study, next: "ca-pr", audience: StudentOnly),
            Pathway([Factor("work.years_skilled", 1, null, points: 50)], cutoff: 100, slug: "ca-pr",
                requirements: [Requirement("english.level.CLB", Op.Gte, "7", "CLB")]));

        var pr = result.Results.Single(r => r.PathwaySlug == "ca-pr");
        Assert.Equal("XX", pr.CountryCode);
        Assert.Equal(Grade.NotEligible, pr.Grade);
        Assert.Equal(0, pr.Points!.Total);
        Assert.Equal("Not provided", Assert.Single(pr.Requirements).Current);
        Assert.Contains(pr.Gaps, g => g.Field == "english.level.CLB");
        Assert.Equal(["ca-study", "ca-pr"], pr.Chain);
        Assert.Equal(["ca-study", "ca-pr"], result.Results.Single(r => r.PathwaySlug == "ca-study").Chain);
    }

    [Fact]
    public void A_pathway_outside_any_chain_has_only_itself_as_chain()
    {
        Assert.Equal(["xx-pr"], Assert.Single(Evaluate(Developer(), Pathway(slug: "xx-pr")).Results).Chain);
    }

    [Fact]
    public void Sources_are_listed_once_per_url_with_the_oldest_verification()
    {
        var oldSource = new RuleSource("https://example.gov/rule", new DateOnly(2026, 1, 5));
        var pathway = Pathway(slug: "xx-pr", requirements:
        [
            Requirement("work.years_skilled", Op.Gte, "1", label: "Experience") with { Source = oldSource },
            Requirement("age", Op.Lt, "45", label: "Age"),
        ]);

        var source = Assert.Single(Assert.Single(Evaluate(Developer(), pathway).Results).Sources);

        Assert.Equal(new SourceRef("Test pathway", "https://example.gov/rule", new DateOnly(2026, 1, 5)), source);
    }

    [Fact]
    public void Stale_rules_counts_rows_verified_more_than_90_days_ago()
    {
        var stale = new RuleSource("https://example.gov/old", Today.AddDays(-91));
        var fresh = new RuleSource("https://example.gov/fresh", Today.AddDays(-90));
        var pathway = Pathway([Factor("work.years_skilled", 1, null, points: 5) with { Source = stale }], passMark: 5,
            slug: "xx-pr",
            requirements:
            [
                Requirement("work.years_skilled", Op.Gte, "1") with { Source = stale },
                Requirement("work.years_skilled", Op.Gte, "2") with { Source = fresh },
            ]);

        Assert.Equal(2, Assert.Single(Evaluate(Developer(), pathway).Results).StaleRules);
    }

    [Fact]
    public void Rule_version_comes_from_the_snapshot()
    {
        Assert.Equal(7, Evaluate(Developer(), Pathway(slug: "xx-pr")).RuleVersion);
    }

    [Fact]
    public void Candidate_profile_is_normalised_on_the_assessment_date()
    {
        var pathway = Pathway(slug: "xx-pr", requirements: [Requirement("age", Op.Lt, "31", "years")]);

        var result = EligibilityEngine.Evaluate(Profile(), new RuleSnapshot(1, [], [pathway], Mappings), Rates,
            EngineOptions.Default, Today);

        Assert.Equal("31 years", Assert.Single(Assert.Single(result.Results).Requirements).Current); // born 1995-04-12
    }
}
