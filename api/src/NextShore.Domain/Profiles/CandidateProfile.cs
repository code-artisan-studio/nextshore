namespace NextShore.Domain.Profiles;

/// <summary>User-confirmed profile (API.md <c>CandidateProfile</c>). Country-keyed maps use ISO codes (CA, DE, AU).</summary>
public sealed record CandidateProfile
{
    public required Goal Goal { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Citizenship { get; init; }
    public required EducationProfile Education { get; init; }
    public required WorkProfile Work { get; init; }
    public IReadOnlyDictionary<string, string> OccupationCodes { get; init; } = new Dictionary<string, string>();
    public EnglishProfile? English { get; init; }
    public int? FrenchClb { get; init; }
    public CefrLevel? GermanLevel { get; init; }
    public decimal? FundsUsd { get; init; }
    public required MaritalStatus MaritalStatus { get; init; }
    public SpouseProfile? Spouse { get; init; }
    public IReadOnlyDictionary<string, bool> JobOffer { get; init; } = new Dictionary<string, bool>();
    public decimal? JobOfferSalaryEur { get; init; }
    public IReadOnlyDictionary<string, bool> RelativeIn { get; init; } = new Dictionary<string, bool>();
    public StudentProfile? Student { get; init; }
    public bool? DegreeRecognizedDE { get; init; }
}

public sealed record EducationProfile(EducationLevel Level, string? Field = null, int? Years = null);

public sealed record WorkProfile(decimal YearsSkilled, IReadOnlyDictionary<string, decimal>? YearsInCountry = null);

/// <param name="Scores">Null when no test has been taken.</param>
public sealed record EnglishProfile(EnglishTest Test, EnglishTestVariant? TestVariant, LanguageScores? Scores);

public sealed record LanguageScores(decimal Listening, decimal Reading, decimal Writing, decimal Speaking);

public sealed record SpouseProfile(EducationLevel? EducationLevel = null, int? EnglishClb = null);

public sealed record StudentProfile(decimal? GpaPercent = null, decimal? TuitionBudgetUsd = null);

public enum Goal { Study, Work, Pr }

public enum EducationLevel { Secondary, Diploma, Bachelors, Masters, Phd }

public enum MaritalStatus { Single, Married }

public enum EnglishTest { Ielts, Pte, Toefl, None }

public enum EnglishTestVariant { Academic, General }

public enum CefrLevel { A1, A2, B1, B2, C1, C2 }
