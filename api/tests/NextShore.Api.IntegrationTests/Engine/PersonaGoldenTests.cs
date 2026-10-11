using System.Globalization;
using System.Text;
using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using NextShore.Domain.Profiles;

namespace NextShore.Api.IntegrationTests.Engine;

/// <summary>
/// Golden results for the three demo personas (fictional) on the verified seed rules. Expected values were
/// worked out by hand from docs/RULES-DATA.md; update them deliberately when rules data changes.
/// </summary>
public class PersonaGoldenTests
{
    private static readonly DateOnly Today = new(2026, 10, 10);

    private static readonly ExchangeRates Rates =
        new(new Dictionary<string, decimal> { ["CAD"] = 1.37m, ["EUR"] = 0.92m, ["AUD"] = 1.52m });

    private static readonly Dictionary<string, decimal> NoYearsAbroad = new() { ["CA"] = 0, ["DE"] = 0, ["AU"] = 0 };

    [Fact]
    public void Student_bachelors_graduate_planning_a_masters()
    {
        var student = new CandidateProfile
        {
            Goal = Goal.Study,
            DateOfBirth = new DateOnly(2004, 5, 1),
            Citizenship = "IN",
            Education = new EducationProfile(EducationLevel.Bachelors, "Computer Science", 4),
            Work = new WorkProfile(0.5m, NoYearsAbroad),
            English = new EnglishProfile(EnglishTest.Ielts, EnglishTestVariant.General, new LanguageScores(7.5m, 7.0m, 6.5m, 7.0m)),
            FundsUsd = 30000,
            MaritalStatus = MaritalStatus.Single,
            JobOffer = new Dictionary<string, bool> { ["CA"] = false, ["DE"] = false, ["AU"] = false },
            RelativeIn = new Dictionary<string, bool> { ["CA"] = false, ["AU"] = false },
        };

        AssertGolden(student, """
            ca-pgwp               eligible      100  -
            ca-study-permit       eligible      100  -
            de-student-visa       eligible      100  -
            au-189                not_eligible   91  50/65
            ca-express-entry      not_eligible   71  338/518
            de-opportunity-card   not_eligible   60  0/6
            de-blue-card          not_eligible   40  -
            de-post-study         near_eligible  40  -
            chain CA: ca-study-permit eligible 100 → ca-pgwp eligible 100* → ca-express-entry not_eligible 92* · total ?
            chain DE: de-student-visa eligible 100 → de-post-study eligible 100* → de-blue-card near_eligible 60* · total ?
            """);
    }

    [Fact]
    public void Mid_level_developer_with_a_german_job_offer()
    {
        var developer = new CandidateProfile
        {
            Goal = Goal.Pr,
            DateOfBirth = new DateOnly(1997, 3, 15),
            Citizenship = "IN",
            Education = new EducationProfile(EducationLevel.Bachelors, "Computer Science", 4),
            Work = new WorkProfile(5, NoYearsAbroad),
            English = new EnglishProfile(EnglishTest.Ielts, EnglishTestVariant.General, new LanguageScores(8.0m, 7.0m, 7.0m, 7.5m)),
            GermanLevel = CefrLevel.A2,
            FundsUsd = 25000,
            MaritalStatus = MaritalStatus.Single,
            JobOffer = new Dictionary<string, bool> { ["CA"] = false, ["DE"] = true, ["AU"] = false },
            JobOfferSalaryEur = 52000,
            RelativeIn = new Dictionary<string, bool> { ["CA"] = false, ["AU"] = false },
            DegreeRecognizedDE = true,
        };

        AssertGolden(developer, """
            au-189                eligible      100  75/65
            de-blue-card          eligible      100  -
            ca-express-entry      not_eligible   88  357/518
            de-opportunity-card   not_eligible   87  4/6
            """);
    }

    [Fact]
    public void Senior_developer_married_with_a_sibling_in_canada()
    {
        var senior = new CandidateProfile
        {
            Goal = Goal.Pr,
            DateOfBirth = new DateOnly(1988, 6, 20),
            Citizenship = "IN",
            Education = new EducationProfile(EducationLevel.Masters, "Computer Science", 6),
            Work = new WorkProfile(12, NoYearsAbroad),
            English = new EnglishProfile(EnglishTest.Ielts, EnglishTestVariant.General, new LanguageScores(8.5m, 8.0m, 7.5m, 7.5m)),
            FundsUsd = 40000,
            MaritalStatus = MaritalStatus.Married,
            Spouse = new SpouseProfile(EducationLevel.Bachelors, 7),
            JobOffer = new Dictionary<string, bool> { ["CA"] = false, ["DE"] = false, ["AU"] = false },
            RelativeIn = new Dictionary<string, bool> { ["CA"] = true, ["AU"] = false },
            DegreeRecognizedDE = true,
        };

        AssertGolden(senior, """
            au-189                eligible      100  65/65
            ca-express-entry      not_eligible   87  344/518
            de-opportunity-card   not_eligible   80  3/6
            de-blue-card          near_eligible  60  -
            """);
    }

    private static void AssertGolden(CandidateProfile profile, string expected)
    {
        var result = EligibilityEngine.Evaluate(profile, VerifiedSeedSnapshot.Load(), Rates, EngineOptions.Default, Today);

        Assert.Equal(Normalize(expected), Normalize(Format(result)));
    }

    private static string Format(EngineResult result)
    {
        var text = new StringBuilder();
        foreach (var r in result.Results)
        {
            var points = r.Points is { } p ? $"{p.Total}/{p.Target?.ToString(CultureInfo.InvariantCulture) ?? "?"}" : "-";
            text.AppendLine(CultureInfo.InvariantCulture, $"{r.PathwaySlug,-21} {Grade(r.Grade),-13} {r.ReadinessScore,3}  {points}");
        }

        foreach (var chain in result.Chains)
        {
            var steps = chain.Steps.Select(s => $"{s.PathwaySlug} {Grade(s.Grade)} {s.ReadinessScore}{(s.IsProjected ? "*" : "")}");
            text.AppendLine(CultureInfo.InvariantCulture,
                $"chain {chain.CountryCode}: {string.Join(" → ", steps)} · total {chain.TotalMonths?.ToString(CultureInfo.InvariantCulture) ?? "?"}");
        }

        return text.ToString();
    }

    private static string Grade(Grade grade) => grade switch
    {
        Domain.Enums.Grade.Eligible => "eligible",
        Domain.Enums.Grade.NearEligible => "near_eligible",
        _ => "not_eligible",
    };

    private static string Normalize(string text) =>
        string.Join('\n', text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
}
