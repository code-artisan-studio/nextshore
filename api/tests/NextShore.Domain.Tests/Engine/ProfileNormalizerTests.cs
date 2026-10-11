using NextShore.Domain.Engine;
using NextShore.Domain.Profiles;
using static NextShore.Domain.Tests.Engine.TestRules;

namespace NextShore.Domain.Tests.Engine;

public class ProfileNormalizerTests
{
    private static NormalizedProfile Normalize(CandidateProfile profile) =>
        ProfileNormalizer.Normalize(profile, Mappings, Today);

    [Theory]
    [InlineData("1995-10-10", 31)] // birthday today
    [InlineData("1995-10-11", 30)] // birthday tomorrow
    [InlineData("1996-02-29", 30)] // leap-day birthday
    public void Age_is_whole_years_on_the_assessment_date(string dateOfBirth, int expected)
    {
        var profile = Normalize(Profile(p => p with { DateOfBirth = DateOnly.Parse(dateOfBirth) }));

        Assert.Equal(new ProfileValue.Number(expected), profile.Get("age"));
    }

    [Fact]
    public void Core_fields_map_to_data_model_keys()
    {
        var profile = Normalize(Profile());

        Assert.Equal(new ProfileValue.Text("pr"), profile.Get("goal"));
        Assert.Equal(new ProfileValue.Text("IN"), profile.Get("citizenship"));
        Assert.Equal(new ProfileValue.Text("Computer Science"), profile.Get("education.field"));
        Assert.Equal(new ProfileValue.Number(4), profile.Get("education.years"));
        Assert.Equal(new ProfileValue.Number(5), profile.Get("work.years_skilled"));
        Assert.Equal(new ProfileValue.Number(1.5m), profile.Get("work.years_in_country.AU"));
        Assert.Equal(new ProfileValue.Text("21231"), profile.Get("occupation.code.NOC"));
        Assert.Equal(new ProfileValue.Text("single"), profile.Get("marital_status"));
        Assert.Equal(new ProfileValue.Flag(true), profile.Get("degree.recognized_DE"));
        Assert.Equal(new ProfileValue.Flag(true), profile.Get("job_offer.DE"));
        Assert.Equal(new ProfileValue.Flag(true), profile.Get("relative_in.CA"));
        Assert.Equal(new ProfileValue.Text("ielts"), profile.Get("english.test"));
        Assert.Equal(new ProfileValue.Text("general"), profile.Get("english.test_variant"));
    }

    [Fact]
    public void Ordered_levels_are_ordinals()
    {
        var profile = Normalize(Profile());

        var education = Assert.IsType<ProfileValue.Ordinal>(profile.Get("education.level"));
        Assert.Equal("bachelors", education.Label);
        var german = Assert.IsType<ProfileValue.Ordinal>(profile.Get("german.level.CEFR"));
        Assert.Equal("A2", german.Label);
    }

    [Fact]
    public void Amounts_are_money_in_their_currency()
    {
        var profile = Normalize(Profile());

        Assert.Equal(new ProfileValue.Money(20000, "USD"), profile.Get("funds.amount_usd"));
        Assert.Equal(new ProfileValue.Money(52000, "EUR"), profile.Get("job_offer.salary_eur"));
    }

    [Theory]
    [InlineData(Goal.Study, 5, "student")]
    [InlineData(Goal.Pr, 0.5, "student")]
    [InlineData(Goal.Pr, 1, "developer")]
    [InlineData(Goal.Work, 3, "developer")]
    public void Audience_is_derived_from_goal_and_experience(Goal goal, decimal years, string expected)
    {
        var profile = Normalize(Profile(p => p with { Goal = goal, Work = new WorkProfile(years) }));

        Assert.Equal(new ProfileValue.Text(expected), profile.Get("audience"));
    }

    [Fact]
    public void English_level_per_skill_is_the_highest_level_reached()
    {
        // IELTS General L 8.0 · R 6.0 · W 6.0 · S 7.0
        var profile = Normalize(Profile());

        Assert.Equal(new ProfileValue.Number(9), profile.Get("english.level.CLB.listening"));
        Assert.Equal(new ProfileValue.Number(7), profile.Get("english.level.CLB.reading"));
        Assert.Equal(new ProfileValue.Number(7), profile.Get("english.level.CLB.writing"));
        Assert.Equal(new ProfileValue.Number(9), profile.Get("english.level.CLB.speaking"));
    }

    [Fact]
    public void Overall_english_level_is_the_lowest_skill()
    {
        var profile = Normalize(Profile());

        Assert.Equal(new ProfileValue.Number(7), profile.Get("english.level.CLB"));
        Assert.Equal(new ProfileValue.Number(6), profile.Get("english.level.IELTS"));
        var cefr = Assert.IsType<ProfileValue.Ordinal>(profile.Get("english.level.CEFR"));
        Assert.Equal("B2", cefr.Label);
    }

    [Fact]
    public void Mappings_for_another_test_variant_are_ignored()
    {
        var profile = Normalize(Profile(p => p with
        {
            English = p.English! with { TestVariant = EnglishTestVariant.Academic },
        }));

        Assert.Null(profile.Get("english.level.CLB"));                   // Canada: General Training only
        Assert.Equal(new ProfileValue.Number(6), profile.Get("english.level.IELTS")); // any variant
    }

    [Fact]
    public void Score_below_the_lowest_level_is_below_the_scale()
    {
        var profile = Normalize(Profile(p => p with
        {
            English = p.English! with { Scores = new LanguageScores(4.0m, 6.0m, 6.0m, 6.0m) },
        }));

        Assert.Equal(new ProfileValue.Below("CLB", "4"), profile.Get("english.level.CLB.listening"));
        Assert.Equal(new ProfileValue.Below("CLB", "4"), profile.Get("english.level.CLB"));
        Assert.Equal(new ProfileValue.Below("IELTS", "6"), profile.Get("english.level.IELTS"));
    }

    [Fact]
    public void No_english_test_means_no_english_level()
    {
        var profile = Normalize(Profile(p => p with
        {
            English = new EnglishProfile(EnglishTest.None, null, null),
        }));

        Assert.Equal(new ProfileValue.Text("none"), profile.Get("english.test"));
        Assert.DoesNotContain(profile.Values.Keys, k => k.StartsWith("english.level", StringComparison.Ordinal));
    }

    [Fact]
    public void Optional_sections_are_absent_when_not_given()
    {
        var profile = Normalize(Profile(p => p with
        {
            FrenchClb = null,
            Spouse = null,
            Student = null,
            DegreeRecognizedDE = null,
        }));

        Assert.Null(profile.Get("french.level.CLB"));
        Assert.Null(profile.Get("spouse.education.level"));
        Assert.Null(profile.Get("student.gpa_percent"));
        Assert.Null(profile.Get("degree.recognized_DE"));
    }

    [Fact]
    public void Optional_sections_map_when_given()
    {
        var profile = Normalize(Profile(p => p with
        {
            FrenchClb = 7,
            MaritalStatus = MaritalStatus.Married,
            Spouse = new SpouseProfile(EducationLevel.Masters, 8),
            Student = new StudentProfile(82.5m, 30000),
        }));

        Assert.Equal(new ProfileValue.Number(7), profile.Get("french.level.CLB"));
        Assert.Equal("masters", Assert.IsType<ProfileValue.Ordinal>(profile.Get("spouse.education.level")).Label);
        Assert.Equal(new ProfileValue.Number(8), profile.Get("spouse.english.level.CLB"));
        Assert.Equal(new ProfileValue.Number(82.5m), profile.Get("student.gpa_percent"));
        Assert.Equal(new ProfileValue.Money(30000, "USD"), profile.Get("student.tuition_budget_usd"));
    }

    [Fact]
    public void With_returns_a_changed_copy()
    {
        var original = Normalize(Profile());

        var changed = original.With("work.years_skilled", new ProfileValue.Number(8)).With("goal", null);

        Assert.Equal(new ProfileValue.Number(8), changed.Get("work.years_skilled"));
        Assert.Null(changed.Get("goal"));
        Assert.Equal(new ProfileValue.Number(5), original.Get("work.years_skilled"));
    }
}
