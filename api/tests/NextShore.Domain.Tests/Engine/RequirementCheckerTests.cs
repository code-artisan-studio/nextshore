using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using static NextShore.Domain.Tests.Engine.TestRules;
using Op = NextShore.Domain.Enums.RequirementOperator;

namespace NextShore.Domain.Tests.Engine;

public class RequirementCheckerTests
{
    private static readonly ExchangeRates Rates = new(new Dictionary<string, decimal> { ["CAD"] = 1.37m, ["EUR"] = 0.92m });

    private static NormalizedProfile With(string key, ProfileValue value) =>
        new(new Dictionary<string, ProfileValue> { [key] = value });

    private static RequirementOutcome Check(RequirementRule rule, NormalizedProfile profile) =>
        RequirementChecker.Check(rule, profile, Rates);

    [Theory]
    [InlineData("7", true, "CLB 7")]
    [InlineData("8", false, "CLB 8")]
    public void Gte_reports_met_with_current_and_target(string target, bool met, string expectedTarget)
    {
        var outcome = Check(Requirement("english.level.CLB", Op.Gte, target, "CLB"),
            With("english.level.CLB", new ProfileValue.Number(7)));

        Assert.Equal(met, outcome.Met);
        Assert.Equal("CLB 7", outcome.Current);
        Assert.Equal(expectedTarget, outcome.Target);
    }

    [Theory]
    [InlineData(Op.Eq, 5, "5", true)]
    [InlineData(Op.Eq, 5, "6", false)]
    [InlineData(Op.Neq, 5, "6", true)]
    [InlineData(Op.Neq, 5, "5", false)]
    [InlineData(Op.Gte, 5, "5", true)]
    [InlineData(Op.Gte, 4.9, "5", false)]
    [InlineData(Op.Gt, 5, "5", false)]
    [InlineData(Op.Gt, 5.1, "5", true)]
    [InlineData(Op.Lte, 5, "5", true)]
    [InlineData(Op.Lte, 5.1, "5", false)]
    [InlineData(Op.Lt, 5, "5", false)]
    [InlineData(Op.Lt, 4.5, "5", true)]
    public void Comparison_operators_on_numbers(Op op, double current, string value, bool met)
    {
        var outcome = Check(Requirement("work.years_skilled", op, value, "years"),
            With("work.years_skilled", new ProfileValue.Number((decimal)current)));

        Assert.Equal(met, outcome.Met);
    }

    [Theory]
    [InlineData(Op.Eq, "45 years")]
    [InlineData(Op.Neq, "not 45 years")]
    [InlineData(Op.Gte, "45 years")]
    [InlineData(Op.Gt, "more than 45 years")]
    [InlineData(Op.Lte, "45 years or less")]
    [InlineData(Op.Lt, "under 45 years")]
    public void Target_text_reflects_the_operator(Op op, string expected)
    {
        var outcome = Check(Requirement("age", op, "45", "years"), With("age", new ProfileValue.Number(30)));

        Assert.Equal(expected, outcome.Target);
        Assert.Equal("30 years", outcome.Current);
    }

    [Fact]
    public void In_and_not_in_match_against_listed_values()
    {
        var bachelors = With("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 2));

        var inList = Check(Requirement("education.level", Op.In, """["bachelors","masters","phd"]"""), bachelors);
        var notIn = Check(Requirement("education.level", Op.NotIn, """["secondary","diploma"]"""), bachelors);
        var excluded = Check(Requirement("education.level", Op.NotIn, """["bachelors"]"""), bachelors);

        Assert.True(inList.Met);
        Assert.Equal("one of bachelors, masters, phd", inList.Target);
        Assert.Equal("bachelors", inList.Current);
        Assert.True(notIn.Met);
        Assert.False(excluded.Met);
    }

    [Fact]
    public void Eq_compares_text_and_flags()
    {
        Assert.True(Check(Requirement("goal", Op.Eq, "\"pr\""), With("goal", new ProfileValue.Text("pr"))).Met);
        Assert.False(Check(Requirement("goal", Op.Eq, "\"study\""), With("goal", new ProfileValue.Text("pr"))).Met);
        Assert.True(Check(Requirement("relative_in.CA", Op.Eq, "true"), With("relative_in.CA", new ProfileValue.Flag(true))).Met);
    }

    [Fact]
    public void Exists_is_met_by_any_value()
    {
        var rule = Requirement("education.level", Op.Exists, null);

        var present = Check(rule, With("education.level", new ProfileValue.Ordinal(OrdinalScale.EducationLevel, 0)));
        var missing = Check(rule, new NormalizedProfile(new Dictionary<string, ProfileValue>()));

        Assert.True(present.Met);
        Assert.Equal("Provided", present.Target);
        Assert.False(missing.Met);
    }

    [Theory]
    [InlineData(true, true, "Yes")]
    [InlineData(false, false, "No")]
    public void True_operator_needs_the_flag_set(bool flag, bool met, string current)
    {
        var outcome = Check(Requirement("job_offer.DE", Op.True, null), With("job_offer.DE", new ProfileValue.Flag(flag)));

        Assert.Equal(met, outcome.Met);
        Assert.Equal(current, outcome.Current);
        Assert.Equal("Yes", outcome.Target);
    }

    [Fact]
    public void Missing_field_is_unmet_and_easy_to_fix()
    {
        var rule = Requirement("english.level.CLB", Op.Gte, "7", "CLB", Fixability.Hard);

        var outcome = Check(rule, new NormalizedProfile(new Dictionary<string, ProfileValue>()));

        Assert.False(outcome.Met);
        Assert.Equal("Not provided", outcome.Current);
        Assert.Equal("CLB 7", outcome.Target);
        Assert.Equal(Fixability.Easy, outcome.Fixability);
        Assert.Equal("Add this to your profile", outcome.FixHint);
    }

    [Fact]
    public void Money_is_converted_into_the_rule_currency()
    {
        var outcome = Check(Requirement("funds.amount_usd", Op.Gte, "23448", "CAD"),
            With("funds.amount_usd", new ProfileValue.Money(20000, "USD")));

        Assert.True(outcome.Met); // 20,000 × 1.37 = CAD 27,400
        Assert.Equal("USD 20,000 (≈ CAD 27,400)", outcome.Current);
        Assert.Equal("CAD 23,448", outcome.Target);
    }

    [Fact]
    public void Money_in_the_rule_currency_is_compared_directly()
    {
        var outcome = Check(Requirement("job_offer.salary_eur", Op.Gte, "50700", "EUR"),
            With("job_offer.salary_eur", new ProfileValue.Money(49000, "EUR")));

        Assert.False(outcome.Met);
        Assert.Equal("EUR 49,000", outcome.Current);
        Assert.Equal("EUR 50,700", outcome.Target);
    }

    [Fact]
    public void Missing_exchange_rate_is_a_configuration_error()
    {
        var rule = Requirement("funds.amount_usd", Op.Gte, "29710", "AUD");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            Check(rule, With("funds.amount_usd", new ProfileValue.Money(20000, "USD"))));

        Assert.Contains("AUD", ex.Message);
    }

    [Theory]
    [InlineData("A1", true)]
    [InlineData("A2", true)]
    [InlineData("B1", false)]
    public void Ordinal_levels_compare_by_rank(string target, bool met)
    {
        var outcome = Check(Requirement("german.level.CEFR", Op.Gte, $"\"{target}\"", "CEFR"),
            With("german.level.CEFR", new ProfileValue.Ordinal(OrdinalScale.Cefr, 1)));

        Assert.Equal(met, outcome.Met);
        Assert.Equal("CEFR A2", outcome.Current);
        Assert.Equal($"CEFR {target}", outcome.Target);
    }

    [Fact]
    public void Below_the_scale_is_lower_than_any_level()
    {
        var below = With("english.level.CLB", new ProfileValue.Below("CLB", "4"));

        var gte = Check(Requirement("english.level.CLB", Op.Gte, "4", "CLB"), below);
        var lt = Check(Requirement("english.level.CLB", Op.Lt, "4", "CLB"), below);

        Assert.False(gte.Met);
        Assert.Equal("Below CLB 4", gte.Current);
        Assert.True(lt.Met);
    }

    [Fact]
    public void Ordering_a_text_value_is_a_rule_error()
    {
        var rule = Requirement("goal", Op.Gte, "\"pr\"", label: "Bad rule");

        var ex = Assert.Throws<InvalidOperationException>(() => Check(rule, With("goal", new ProfileValue.Text("pr"))));

        Assert.Contains("Bad rule", ex.Message);
    }

    [Fact]
    public void Unknown_level_label_in_a_rule_is_a_rule_error()
    {
        var rule = Requirement("german.level.CEFR", Op.Gte, "\"Z9\"", "CEFR");

        Assert.Throws<InvalidOperationException>(() =>
            Check(rule, With("german.level.CEFR", new ProfileValue.Ordinal(OrdinalScale.Cefr, 1))));
    }

    [Fact]
    public void Check_all_keeps_rule_order_and_advisory_flag()
    {
        var pathway = new PathwayRule(
            "xx-test", "XX", "Test", PathwayType.Work, [Audience.Developer], false, null, null, null, null, null,
            "Test", 1, Source,
            [
                Requirement("work.years_skilled", Op.Gte, "1", "years", label: "Experience"),
                Requirement("relative_in.CA", Op.True, null, mandatory: false, label: "Advisory"),
            ],
            [], []);
        var profile = new NormalizedProfile(new Dictionary<string, ProfileValue>
        {
            ["work.years_skilled"] = new ProfileValue.Number(3),
        });

        var outcomes = RequirementChecker.CheckAll(pathway, profile, Rates);

        Assert.Equal(["Experience", "Advisory"], outcomes.Select(o => o.Label));
        Assert.True(outcomes[0].Met);
        Assert.False(outcomes[1].Met);
        Assert.False(outcomes[1].IsMandatory);
    }
}
