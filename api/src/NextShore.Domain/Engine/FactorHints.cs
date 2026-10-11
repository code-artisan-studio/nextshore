using NextShore.Domain.Enums;

namespace NextShore.Domain.Engine;

/// <summary>Display label, unit and default fix for a profile field key. Generic product copy, not visa rules.</summary>
public sealed record FactorHint(string Label, string? Unit, Fixability Fixability, string FixHint, int? FixMonths);

public static class FactorHints
{
    public static FactorHint For(string key)
    {
        var parts = key.Split('.');
        return parts switch
        {
            ["english", "level", var scale, var skill] => new($"English {skill}", scale, Fixability.Easy, "Retake your English test", 2),
            ["english", "level", var scale] => new("English", scale, Fixability.Easy, "Retake your English test", 2),
            ["french", "level", var scale] => new("French", scale, Fixability.Medium, "Improve your French test result", 6),
            ["german", "level", var scale] => new("German", scale, Fixability.Medium, "Take a German course and certificate", 6),
            ["spouse", "english", "level", var scale] => new("Spouse's English", scale, Fixability.Easy, "Spouse retakes their English test", 2),
            ["spouse", "education", "level"] => new("Spouse's education", null, Fixability.Hard, "Spouse completes a higher qualification", 24),
            ["age"] => new("Age", "years", Fixability.Impossible, "Age cannot be changed", null),
            ["education", "level"] => new("Education", null, Fixability.Hard, "Complete a higher qualification", 24),
            ["work", "years_skilled"] => new("Skilled work experience", "years", Fixability.Medium, "Gain more skilled work experience", 12),
            ["work", "years_in_country", var country] => new($"Work experience in {country}", "years", Fixability.Medium, $"Gain work experience in {country}", 12),
            _ => new(key, null, Fixability.Medium, "Improve this factor", null),
        };
    }

    /// <summary>Age and work experience count whole years only.</summary>
    public static bool IsYearCount(string key) => key == "age" || key.StartsWith("work.", StringComparison.Ordinal);
}
