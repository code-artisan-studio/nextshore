namespace NextShore.Domain.Entities;

/// <summary>Anonymous saved assessment. No name, email or contact info; deleted after <see cref="ExpiresAt"/>.</summary>
public class Assessment
{
    public Guid Id { get; set; }

    /// <summary>User-confirmed <c>CandidateProfile</c> as raw JSON.</summary>
    public required string Profile { get; set; }

    /// <summary>Engine output (<c>PathwayResult[]</c> + chains) as raw JSON.</summary>
    public required string Results { get; set; }

    public int RuleVersion { get; set; }
    public string? Summary { get; set; }
    public DateTimeOffset? SummaryGeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
