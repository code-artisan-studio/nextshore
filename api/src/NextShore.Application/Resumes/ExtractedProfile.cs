namespace NextShore.Application.Resumes;

// ExtractedProfile (DATA-MODEL.md): what the AI read from a resume, for the user to confirm. Every confidence is 0–1;
// the UI highlights values below 0.7. Never stored.

public sealed record ExtractedProfile(
    IReadOnlyList<ExtractedEducation> Education,
    IReadOnlyList<ExtractedWork> Work,
    ExtractedYears TotalSkilledYears,
    IReadOnlyList<string> Skills,
    IReadOnlyList<ExtractedLanguage> LanguagesMentioned,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<OccupationSuggestion> OccupationSuggestions);

/// <param name="Level"><c>secondary</c> · <c>diploma</c> · <c>bachelors</c> · <c>masters</c> · <c>phd</c>.</param>
public sealed record ExtractedEducation(
    string Level, string? Field, string? InstitutionCountry, int? GraduationYear, decimal Confidence);

/// <param name="StartDate"><c>YYYY-MM</c>.</param>
/// <param name="EndDate"><c>YYYY-MM</c>; null when current.</param>
public sealed record ExtractedWork(
    string? Title, string? Employer, string? StartDate, string? EndDate, string? Country, bool IsSkilled, decimal Confidence);

public sealed record ExtractedYears(decimal? Value, decimal Confidence);

public sealed record ExtractedLanguage(string Language, string? Level, decimal Confidence);

/// <param name="System"><c>NOC</c> · <c>ISCO</c> · <c>ANZSCO</c>.</param>
public sealed record OccupationSuggestion(string System, string Code, string Title, decimal Confidence);
