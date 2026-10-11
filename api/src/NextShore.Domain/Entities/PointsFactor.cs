using NextShore.Domain.Enums;

namespace NextShore.Domain.Entities;

public class PointsFactor : SourcedRuleEntity
{
    public Guid PathwayId { get; set; }
    public Pathway? Pathway { get; set; }

    /// <summary>Profile field key, e.g. <c>age</c>.</summary>
    public required string Factor { get; set; }
    public PointsVariant Variant { get; set; }

    /// <summary>Inclusive bracket bounds; null = open.</summary>
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    /// <summary>For categorical factors, e.g. <c>masters</c> for <c>education.level</c>.</summary>
    public string? MatchValue { get; set; }

    public int? Points { get; set; }
    public int? MaxPointsForFactor { get; set; }
}
