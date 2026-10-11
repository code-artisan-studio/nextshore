using System.Globalization;
using NextShore.Domain.Profiles;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Engine;

/// <summary>Turns a <see cref="CandidateProfile"/> into the flat field-key map the engine evaluates.</summary>
public static class ProfileNormalizer
{
    /// <summary>Below this much skilled experience a non-study goal is still treated as a student.</summary>
    public const decimal DeveloperMinYears = 1;

    private static readonly string[] Skills = ["listening", "reading", "writing", "speaking"];

    public static NormalizedProfile Normalize(
        CandidateProfile profile, IReadOnlyList<LanguageMappingRule> mappings, DateOnly today)
    {
        var values = new Dictionary<string, ProfileValue>();
        void Set(string key, ProfileValue? value)
        {
            if (value is not null)
            {
                values[key] = value;
            }
        }

        Set("goal", Text(profile.Goal));
        Set("audience", new ProfileValue.Text(
            profile.Goal == Goal.Study || profile.Work.YearsSkilled < DeveloperMinYears ? "student" : "developer"));
        Set("age", new ProfileValue.Number(AgeOn(profile.DateOfBirth, today)));
        Set("citizenship", new ProfileValue.Text(profile.Citizenship));

        Set("education.level", Education(profile.Education.Level));
        Set("education.field", profile.Education.Field is { } field ? new ProfileValue.Text(field) : null);
        Set("education.years", Number(profile.Education.Years));

        Set("work.years_skilled", new ProfileValue.Number(profile.Work.YearsSkilled));
        foreach (var (country, years) in profile.Work.YearsInCountry ?? new Dictionary<string, decimal>())
        {
            Set($"work.years_in_country.{country}", new ProfileValue.Number(years));
        }

        foreach (var (system, code) in profile.OccupationCodes)
        {
            Set($"occupation.code.{system}", new ProfileValue.Text(code));
        }

        if (profile.English is { } english)
        {
            AddEnglish(english, mappings, Set);
        }

        Set("french.level.CLB", Number(profile.FrenchClb));
        Set("german.level.CEFR", profile.GermanLevel is { } german
            ? new ProfileValue.Ordinal(OrdinalScale.Cefr, (int)german)
            : null);
        Set("funds.amount_usd", Money(profile.FundsUsd, ExchangeRates.Usd));

        Set("marital_status", Text(profile.MaritalStatus));
        if (profile.Spouse is { } spouse)
        {
            Set("spouse.education.level", spouse.EducationLevel is { } level ? Education(level) : null);
            Set("spouse.english.level.CLB", Number(spouse.EnglishClb));
        }

        foreach (var (country, hasOffer) in profile.JobOffer)
        {
            Set($"job_offer.{country}", new ProfileValue.Flag(hasOffer));
        }

        Set("job_offer.salary_eur", Money(profile.JobOfferSalaryEur, "EUR"));
        foreach (var (country, hasRelative) in profile.RelativeIn)
        {
            Set($"relative_in.{country}", new ProfileValue.Flag(hasRelative));
        }

        if (profile.Student is { } student)
        {
            Set("student.gpa_percent", Number(student.GpaPercent));
            Set("student.tuition_budget_usd", Money(student.TuitionBudgetUsd, ExchangeRates.Usd));
        }

        Set("degree.recognized_DE", profile.DegreeRecognizedDE is { } recognized ? new ProfileValue.Flag(recognized) : null);

        return new NormalizedProfile(values);
    }

    private static void AddEnglish(
        EnglishProfile english, IReadOnlyList<LanguageMappingRule> mappings, Action<string, ProfileValue?> set)
    {
        var test = english.Test.ToString().ToLowerInvariant();
        var variant = english.TestVariant?.ToString().ToLowerInvariant();
        set("english.test", new ProfileValue.Text(test));
        set("english.test_variant", variant is null ? null : new ProfileValue.Text(variant));
        if (english.Test == EnglishTest.None || english.Scores is not { } scores)
        {
            return;
        }

        var scoreBySkill = new Dictionary<string, decimal>
        {
            ["listening"] = scores.Listening,
            ["reading"] = scores.Reading,
            ["writing"] = scores.Writing,
            ["speaking"] = scores.Speaking,
        };
        foreach (var (skill, score) in scoreBySkill)
        {
            set($"english.score.{skill}", new ProfileValue.Number(score));
        }

        var applicable = mappings.Where(m => m.Test == test && (m.TestVariant is null || m.TestVariant == variant));
        foreach (var scale in applicable.GroupBy(m => m.TargetScale))
        {
            var levelsBySkill = scale.ToLookup(m => m.Skill);
            var perSkill = new List<(ProfileValue Value, decimal Order)>();
            foreach (var skill in Skills.Where(levelsBySkill.Contains))
            {
                var level = LevelReached(scale.Key, levelsBySkill[skill].ToList(), scoreBySkill[skill]);
                set($"english.level.{scale.Key}.{skill}", level.Value);
                perSkill.Add(level);
            }

            // Overall = lowest skill; only meaningful when every skill has a mapping.
            if (perSkill.Count == Skills.Length)
            {
                set($"english.level.{scale.Key}", perSkill.MinBy(l => l.Order).Value);
            }
        }
    }

    /// <summary>Highest level whose minimum score is met, or <see cref="ProfileValue.Below"/> the lowest level.</summary>
    private static (ProfileValue Value, decimal Order) LevelReached(
        string scale, IReadOnlyList<LanguageMappingRule> levels, decimal score)
    {
        var ordinal = OrdinalScale.Find(scale);
        var ordered = levels.Select(m => (Mapping: m, Order: LevelOrder(scale, ordinal, m.TargetLevel)))
            .OrderBy(l => l.Order)
            .ToList();

        var reached = ordered.LastOrDefault(l => score >= l.Mapping.MinScore);
        if (reached.Mapping is null)
        {
            return (new ProfileValue.Below(scale, ordered[0].Mapping.TargetLevel), decimal.MinValue);
        }

        ProfileValue value = ordinal is null
            ? new ProfileValue.Number(reached.Order)
            : new ProfileValue.Ordinal(ordinal, (int)reached.Order);
        return (value, reached.Order);
    }

    private static decimal LevelOrder(string scale, OrdinalScale? ordinal, string label)
    {
        if (ordinal is not null)
        {
            return ordinal.RankOf(label)
                ?? throw new InvalidOperationException($"Language mapping level '{label}' is not on the {scale} scale.");
        }

        return decimal.TryParse(label, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw new InvalidOperationException($"Language mapping level '{label}' on {scale} is not a number.");
    }

    private static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        return today < dateOfBirth.AddYears(age) ? age - 1 : age;
    }

    private static ProfileValue.Text Text<TEnum>(TEnum value)
        where TEnum : struct, Enum => new(value.ToString().ToLowerInvariant());

    private static ProfileValue.Ordinal Education(EducationLevel level) => new(OrdinalScale.EducationLevel, (int)level);

    private static ProfileValue.Number? Number(decimal? value) => value is { } v ? new(v) : null;

    private static ProfileValue.Money? Money(decimal? amount, string currency) =>
        amount is { } a ? new(a, currency) : null;
}
