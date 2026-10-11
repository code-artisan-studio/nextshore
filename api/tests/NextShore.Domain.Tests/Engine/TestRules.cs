using NextShore.Domain.Enums;
using NextShore.Domain.Profiles;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Tests.Engine;

/// <summary>Builders for engine tests. Mapping values follow the seeded IELTS tables.</summary>
internal static class TestRules
{
    public static readonly DateOnly Today = new(2026, 10, 10);
    public static readonly RuleSource Source = new("https://example.gov/rule", Today);

    public static IReadOnlyList<LanguageMappingRule> Mappings { get; } =
    [
        // IELTS General Training → CLB (Canada; general only)
        .. Level("ielts", "general", "CLB", "4", 4.5m, 3.5m, 4.0m, 4.0m),
        .. Level("ielts", "general", "CLB", "5", 5.0m, 4.0m, 5.0m, 5.0m),
        .. Level("ielts", "general", "CLB", "6", 5.5m, 5.0m, 5.5m, 5.5m),
        .. Level("ielts", "general", "CLB", "7", 6.0m, 6.0m, 6.0m, 6.0m),
        .. Level("ielts", "general", "CLB", "8", 7.5m, 6.5m, 6.5m, 6.5m),
        .. Level("ielts", "general", "CLB", "9", 8.0m, 7.0m, 7.0m, 7.0m),
        .. Level("ielts", "general", "CLB", "10", 8.5m, 8.0m, 7.5m, 7.5m),
        // IELTS → Australian levels (any variant)
        .. Level("ielts", null, "IELTS", "6", 6.0m, 6.0m, 6.0m, 6.0m),
        .. Level("ielts", null, "IELTS", "7", 7.0m, 7.0m, 7.0m, 7.0m),
        .. Level("ielts", null, "IELTS", "8", 8.0m, 8.0m, 8.0m, 8.0m),
        // IELTS → CEFR (test data only)
        .. Level("ielts", null, "CEFR", "B1", 4.0m, 4.0m, 4.0m, 4.0m),
        .. Level("ielts", null, "CEFR", "B2", 5.5m, 5.5m, 5.5m, 5.5m),
        .. Level("ielts", null, "CEFR", "C1", 7.0m, 7.0m, 7.0m, 7.0m),
    ];

    private static IEnumerable<LanguageMappingRule> Level(
        string test, string? variant, string scale, string level, decimal l, decimal r, decimal w, decimal s) =>
        new[] { ("listening", l), ("reading", r), ("writing", w), ("speaking", s) }
            .Select(x => new LanguageMappingRule(test, variant, x.Item1, x.Item2, scale, level, Source));

    public static CandidateProfile Profile(Func<CandidateProfile, CandidateProfile>? change = null)
    {
        var profile = new CandidateProfile
        {
            Goal = Goal.Pr,
            DateOfBirth = new DateOnly(1995, 4, 12),
            Citizenship = "IN",
            Education = new EducationProfile(EducationLevel.Bachelors, "Computer Science", 4),
            Work = new WorkProfile(5, new Dictionary<string, decimal> { ["CA"] = 0, ["DE"] = 0, ["AU"] = 1.5m }),
            OccupationCodes = new Dictionary<string, string> { ["NOC"] = "21231", ["ANZSCO"] = "261313" },
            English = new EnglishProfile(EnglishTest.Ielts, EnglishTestVariant.General, new LanguageScores(8.0m, 6.0m, 6.0m, 7.0m)),
            GermanLevel = CefrLevel.A2,
            FundsUsd = 20000,
            MaritalStatus = MaritalStatus.Single,
            JobOffer = new Dictionary<string, bool> { ["CA"] = false, ["DE"] = true, ["AU"] = false },
            JobOfferSalaryEur = 52000,
            RelativeIn = new Dictionary<string, bool> { ["CA"] = true, ["AU"] = false },
            DegreeRecognizedDE = true,
        };
        return change is null ? profile : change(profile);
    }

    public static PointsFactorRule Factor(
        string factor,
        decimal? min = null,
        decimal? max = null,
        string? match = null,
        int points = 0,
        PointsVariant variant = PointsVariant.Default,
        int? cap = null) =>
        new(factor, variant, min, max, match, points, cap, Source);

    public static PathwayRule Pathway(
        IReadOnlyList<PointsFactorRule>? factors = null,
        int? passMark = null,
        int? cutoff = null,
        IReadOnlyList<RequirementRule>? requirements = null,
        string slug = "xx-test",
        PathwayType type = PathwayType.Pr,
        string country = "XX",
        string? next = null,
        int? durationMonths = null,
        IReadOnlyList<Audience>? audience = null) =>
        new(slug, country, "Test pathway", type, audience ?? [Audience.Developer], factors is { Count: > 0 }, passMark,
            cutoff, null, next, durationMonths, "Test", 1, Source, requirements ?? [], factors ?? [], []);

    public static RequirementRule Requirement(
        string field,
        RequirementOperator op,
        string? value,
        string? unit = null,
        Fixability fixability = Fixability.Medium,
        bool mandatory = true,
        string label = "Test requirement") =>
        new(Guid.NewGuid(), field, op, value, unit, mandatory, fixability, "Fix it", 3, label, Source);
}
