namespace NextShore.Domain.Entities;

/// <summary>Maps a test score to a level on a country's scale, e.g. IELTS listening 8.0 → CLB 9.</summary>
public class LanguageScoreMapping : SourcedRuleEntity
{
    /// <summary><c>ielts</c> · <c>pte</c> · <c>toefl</c>.</summary>
    public required string Test { get; set; }

    /// <summary><c>listening</c> · <c>reading</c> · <c>writing</c> · <c>speaking</c>.</summary>
    public required string Skill { get; set; }
    public decimal? MinScore { get; set; }

    /// <summary><c>CLB</c> · <c>CEFR</c> · <c>IELTS</c>.</summary>
    public required string TargetScale { get; set; }
    public required string TargetLevel { get; set; }
}
