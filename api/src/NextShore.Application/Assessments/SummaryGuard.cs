using System.Text.RegularExpressions;

namespace NextShore.Application.Assessments;

/// <summary>
/// Deterministic checks on AI summaries (CLAUDE.md rule 4, UI.md copy rules): no success odds or guarantees, and at
/// most <see cref="MaxWords"/> words.
/// </summary>
public static partial class SummaryGuard
{
    public const int MaxWords = 200;

    /// <summary>The first banned phrase found, or null.</summary>
    public static string? FindBannedPhrase(string text) =>
        BannedPhrases().Match(text) is { Success: true } match ? match.Value : null;

    /// <summary>Text within <see cref="MaxWords"/> words, cut after the last whole sentence that fits; null if none fits.</summary>
    public static string? FitToLimit(string text)
    {
        var trimmed = text.Trim();
        if (Words().Count(trimmed) <= MaxWords)
        {
            return trimmed;
        }

        var cut = SentenceEnds().Matches(trimmed)
            .Select(m => trimmed[..(m.Index + m.Length)].TrimEnd())
            .LastOrDefault(prefix => Words().Count(prefix) <= MaxWords);
        return cut;
    }

    [GeneratedRegex(@"\b(chances?|probab(ility|ilities|le|ly)|likelihood|likely to (get|be approved)|odds|guarantee[sd]?|you will (get|be approved|qualify))\b|%",
        RegexOptions.IgnoreCase)]
    private static partial Regex BannedPhrases();

    [GeneratedRegex(@"\S+")]
    private static partial Regex Words();

    [GeneratedRegex(@"[.!?](?=\s|$)")]
    private static partial Regex SentenceEnds();
}
