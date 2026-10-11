namespace NextShore.Application.Assessments;

/// <summary>Config section <c>Assessments</c>.</summary>
public sealed class AssessmentSettings
{
    public const string Section = "Assessments";

    /// <summary>Days an assessment (and its share link) lives before deletion.</summary>
    public int LifetimeDays { get; set; } = 30;

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}
