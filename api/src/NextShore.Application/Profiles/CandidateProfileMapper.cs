using System.Text.Json;
using NextShore.Domain.Profiles;

namespace NextShore.Application.Profiles;

/// <summary>Maps a validated request to the Domain profile.</summary>
public static class CandidateProfileMapper
{
    public static CandidateProfile ToDomain(CandidateProfileRequest request) => new()
    {
        Goal = Parse<Goal>(request.Goal!),
        DateOfBirth = request.DateOfBirth!.Value,
        Citizenship = request.Citizenship!,
        Education = new EducationProfile(Parse<EducationLevel>(request.Education!.Level!), request.Education.Field, request.Education.Years),
        Work = new WorkProfile(request.Work!.YearsSkilled!.Value, request.Work.YearsInCountry),
        OccupationCodes = request.OccupationCodes?.ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value)
            ?? new Dictionary<string, string>(),
        English = request.English is { } e
            ? new EnglishProfile(
                Parse<EnglishTest>(e.Test!),
                e.TestVariant is null ? null : Parse<EnglishTestVariant>(e.TestVariant),
                e.Scores is { } s ? new LanguageScores(s.Listening!.Value, s.Reading!.Value, s.Writing!.Value, s.Speaking!.Value) : null)
            : null,
        FrenchClb = request.French?.Clb,
        GermanLevel = request.German?.Level is { } german ? Parse<CefrLevel>(german) : null,
        FundsUsd = request.FundsUsd,
        MaritalStatus = Parse<MaritalStatus>(request.MaritalStatus!),
        Spouse = request.Spouse is { } spouse
            ? new SpouseProfile(spouse.EducationLevel is null ? null : Parse<EducationLevel>(spouse.EducationLevel), spouse.EnglishClb)
            : null,
        JobOffer = request.JobOffer?.Countries?.ToDictionary(kv => kv.Key, kv => kv.Value.ValueKind == JsonValueKind.True)
            ?? new Dictionary<string, bool>(),
        JobOfferSalaryEur = request.JobOffer?.SalaryEur,
        RelativeIn = request.RelativeIn ?? new Dictionary<string, bool>(),
        Student = request.Student is { } student ? new StudentProfile(student.GpaPercent, student.TuitionBudgetUsd) : null,
        DegreeRecognizedDE = request.DegreeRecognizedDE,
    };

    private static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum => Enum.Parse<TEnum>(value, ignoreCase: true);
}
