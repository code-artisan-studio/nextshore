using NextShore.Domain.Enums;

namespace NextShore.Domain.Entities;

public class Pathway : SourcedRuleEntity
{
    public Guid CountryId { get; set; }
    public Country? Country { get; set; }

    /// <summary>Stable key, e.g. <c>ca-express-entry</c>.</summary>
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public PathwayType Type { get; set; }
    public List<Audience> Audience { get; set; } = [];
    public bool IsPointsBased { get; set; }

    /// <summary>Fixed pass mark (e.g. AU 189 minimum).</summary>
    public int? PassMark { get; set; }

    /// <summary>Latest observed invitation cutoff.</summary>
    public int? Cutoff { get; set; }
    public DateOnly? CutoffAsOf { get; set; }

    public Guid? NextPathwayId { get; set; }
    public Pathway? NextPathway { get; set; }

    public int? TypicalDurationMonths { get; set; }
    public required string Summary { get; set; }

    /// <summary>Bumped on publish.</summary>
    public int RuleVersion { get; set; }

    public List<Requirement> Requirements { get; set; } = [];
    public List<PointsFactor> PointsFactors { get; set; } = [];
    public List<OccupationListEntry> Occupations { get; set; } = [];
}
