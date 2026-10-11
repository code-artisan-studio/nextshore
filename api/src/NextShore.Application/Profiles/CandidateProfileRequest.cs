using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextShore.Application.Profiles;

// JSON contract of CandidateProfile (API.md). Everything is nullable so the validator, not the JSON reader,
// reports what is missing. Enum-like values are snake_case strings.

public sealed record CandidateProfileRequest
{
    public string? Goal { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Citizenship { get; init; }
    public EducationRequest? Education { get; init; }
    public WorkRequest? Work { get; init; }
    public Dictionary<string, string>? OccupationCodes { get; init; }
    public EnglishRequest? English { get; init; }
    public FrenchRequest? French { get; init; }
    public GermanRequest? German { get; init; }
    public decimal? FundsUsd { get; init; }
    public string? MaritalStatus { get; init; }
    public SpouseRequest? Spouse { get; init; }
    public JobOfferRequest? JobOffer { get; init; }
    public Dictionary<string, bool>? RelativeIn { get; init; }
    public StudentRequest? Student { get; init; }
    public bool? DegreeRecognizedDE { get; init; }
}

public sealed record EducationRequest(string? Level, string? Field = null, int? Years = null);

public sealed record WorkRequest(decimal? YearsSkilled, Dictionary<string, decimal>? YearsInCountry = null);

public sealed record EnglishRequest(string? Test, string? TestVariant = null, ScoresRequest? Scores = null);

public sealed record ScoresRequest(decimal? Listening, decimal? Reading, decimal? Writing, decimal? Speaking);

public sealed record FrenchRequest(int? Clb);

public sealed record GermanRequest(string? Level);

public sealed record SpouseRequest(string? EducationLevel = null, int? EnglishClb = null);

public sealed record StudentRequest(decimal? GpaPercent = null, decimal? TuitionBudgetUsd = null);

/// <summary><c>{ "CA": false, "DE": true, "salaryEur": 52000 }</c>: country codes map to yes/no.</summary>
public sealed record JobOfferRequest
{
    public decimal? SalaryEur { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Countries { get; init; }
}
