using System.Text.RegularExpressions;
using FluentValidation;

namespace NextShore.Application.Profiles;

public sealed partial class CandidateProfileValidator : AbstractValidator<CandidateProfileRequest>
{
    public static readonly string[] Goals = ["study", "work", "pr"];
    public static readonly string[] EducationLevels = ["secondary", "diploma", "bachelors", "masters", "phd"];
    public static readonly string[] MaritalStatuses = ["single", "married"];
    public static readonly string[] EnglishTests = ["ielts", "pte", "toefl", "none"];
    public static readonly string[] TestVariants = ["academic", "general"];
    public static readonly string[] CefrLevels = ["A1", "A2", "B1", "B2", "C1", "C2"];
    public static readonly string[] OccupationSystems = ["NOC", "ISCO", "ANZSCO"];

    public CandidateProfileValidator(TimeProvider clock)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        RuleFor(p => p.Goal).NotEmpty().Must(OneOf(Goals)).WithMessage(Expected(Goals));
        RuleFor(p => p.DateOfBirth).NotNull()
            .Must(d => d < today.AddYears(-15) && d > today.AddYears(-100))
            .WithMessage("Must be a date of birth for someone aged 15 to 100.");
        RuleFor(p => p.Citizenship).NotEmpty().Matches(CountryCode()).WithMessage("Must be an ISO 3166 alpha-2 code, e.g. IN.");

        RuleFor(p => p.Education).NotNull();
        RuleFor(p => p.Education!).ChildRules(e =>
        {
            e.RuleFor(x => x.Level).NotEmpty().Must(OneOf(EducationLevels)).WithMessage(Expected(EducationLevels));
            e.RuleFor(x => x.Field).MaximumLength(200);
            e.RuleFor(x => x.Years).InclusiveBetween(0, 15);
        }).When(p => p.Education is not null);

        RuleFor(p => p.Work).NotNull();
        RuleFor(p => p.Work!).ChildRules(w =>
        {
            w.RuleFor(x => x.YearsSkilled).NotNull().InclusiveBetween(0, 60);
        }).When(p => p.Work is not null);
        RuleFor(p => p.Work!.YearsInCountry).Custom((years, context) => EachCountry(years, "work.yearsInCountry", context,
            value => value is >= 0 and <= 60 ? null : "Must be between 0 and 60.")).When(p => p.Work is not null);

        RuleFor(p => p.OccupationCodes).Custom((codes, context) =>
        {
            foreach (var (system, code) in codes ?? [])
            {
                if (!OneOf(OccupationSystems)(system))
                {
                    context.AddFailure($"occupationCodes.{system}", Expected(OccupationSystems));
                }
                else if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
                {
                    context.AddFailure($"occupationCodes.{system}", "Must be a code of 1 to 20 characters.");
                }
            }
        });

        RuleFor(p => p.English!).SetValidator(new EnglishValidator()).When(p => p.English is not null);
        RuleFor(p => p.French!.Clb).NotNull().InclusiveBetween(1, 12).When(p => p.French is not null);
        RuleFor(p => p.German!.Level).NotEmpty().Must(OneOf(CefrLevels)).WithMessage(Expected(CefrLevels))
            .When(p => p.German is not null);
        RuleFor(p => p.FundsUsd).GreaterThanOrEqualTo(0);

        RuleFor(p => p.MaritalStatus).NotEmpty().Must(OneOf(MaritalStatuses)).WithMessage(Expected(MaritalStatuses));
        RuleFor(p => p.Spouse).Null().WithMessage("Only allowed when maritalStatus is married.")
            .When(p => p.MaritalStatus is not null && !string.Equals(p.MaritalStatus, "married", StringComparison.OrdinalIgnoreCase));
        RuleFor(p => p.Spouse!).ChildRules(s =>
        {
            s.RuleFor(x => x.EducationLevel).Must(OneOf(EducationLevels)).WithMessage(Expected(EducationLevels))
                .When(x => x.EducationLevel is not null);
            s.RuleFor(x => x.EnglishClb).InclusiveBetween(1, 12);
        }).When(p => p.Spouse is not null);

        RuleFor(p => p.JobOffer!.SalaryEur).GreaterThanOrEqualTo(0).When(p => p.JobOffer is not null);
        RuleFor(p => p.JobOffer!.Countries).Custom((offers, context) => EachCountry(offers, "jobOffer", context,
            value => value.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
                ? null
                : "Must be true or false.")).When(p => p.JobOffer is not null);
        RuleFor(p => p.RelativeIn).Custom((relatives, context) => EachCountry(relatives, "relativeIn", context, _ => null));

        RuleFor(p => p.Student!).ChildRules(s =>
        {
            s.RuleFor(x => x.GpaPercent).InclusiveBetween(0, 100);
            s.RuleFor(x => x.TuitionBudgetUsd).GreaterThanOrEqualTo(0);
        }).When(p => p.Student is not null);
    }

    /// <summary>Validates a country-keyed map, reporting errors as <c>{path}.{countryCode}</c>.</summary>
    private static void EachCountry<TValue>(
        Dictionary<string, TValue>? map, string path, ValidationContext<CandidateProfileRequest> context,
        Func<TValue, string?> checkValue)
    {
        foreach (var (country, value) in map ?? [])
        {
            if (!CountryCode().IsMatch(country))
            {
                context.AddFailure($"{path}.{country}", "Country keys must be ISO alpha-2 codes, e.g. CA.");
            }
            else if (checkValue(value) is { } message)
            {
                context.AddFailure($"{path}.{country}", message);
            }
        }
    }

    internal static Func<string?, bool> OneOf(string[] allowed) =>
        value => value is null || allowed.Contains(value, StringComparer.OrdinalIgnoreCase);

    internal static string Expected(string[] allowed) => $"Must be one of: {string.Join(", ", allowed)}.";

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryCode();

    private sealed class EnglishValidator : AbstractValidator<EnglishRequest>
    {
        // Per-skill score ranges and steps for each test.
        private static readonly Dictionary<string, (decimal Min, decimal Max, decimal Step)> Ranges = new()
        {
            ["ielts"] = (0, 9, 0.5m),
            ["pte"] = (10, 90, 1),
            ["toefl"] = (0, 30, 1),
        };

        public EnglishValidator()
        {
            RuleFor(e => e.Test).NotEmpty().Must(OneOf(EnglishTests)).WithMessage(Expected(EnglishTests));
            RuleFor(e => e.TestVariant).NotEmpty().Must(OneOf(TestVariants)).WithMessage(Expected(TestVariants))
                .When(e => IsTest(e, "ielts") || IsTest(e, "pte"))
                .WithMessage("Required for IELTS (academic or general) and PTE (academic or general = PTE Core).");
            RuleFor(e => e.Scores).NotNull().When(e => e.Test is not null && !IsTest(e, "none"))
                .WithMessage("Scores are required when a test was taken.");
            RuleFor(e => e.Scores).Null().When(e => IsTest(e, "none"))
                .WithMessage("Leave scores empty when no test was taken.");

            When(e => e.Scores is not null && e.Test is not null && Ranges.ContainsKey(e.Test.ToLowerInvariant()), () =>
            {
                RuleFor(e => e.Scores!.Listening).Must(BeAScore).WithMessage(RangeMessage);
                RuleFor(e => e.Scores!.Reading).Must(BeAScore).WithMessage(RangeMessage);
                RuleFor(e => e.Scores!.Writing).Must(BeAScore).WithMessage(RangeMessage);
                RuleFor(e => e.Scores!.Speaking).Must(BeAScore).WithMessage(RangeMessage);
            });
        }

        private static bool IsTest(EnglishRequest e, string test) => string.Equals(e.Test, test, StringComparison.OrdinalIgnoreCase);

        private static bool BeAScore(EnglishRequest english, decimal? score)
        {
            var (min, max, step) = Ranges[english.Test!.ToLowerInvariant()];
            return score is { } s && s >= min && s <= max && (s - min) % step == 0;
        }

        private static string RangeMessage(EnglishRequest english)
        {
            var (min, max, step) = Ranges[english.Test!.ToLowerInvariant()];
            return $"Must be a {english.Test!.ToUpperInvariant()} score from {min} to {max} in steps of {step}.";
        }
    }
}
