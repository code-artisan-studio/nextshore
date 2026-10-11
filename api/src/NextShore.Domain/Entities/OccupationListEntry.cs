namespace NextShore.Domain.Entities;

public class OccupationListEntry : SourcedRuleEntity
{
    public Guid PathwayId { get; set; }
    public Pathway? Pathway { get; set; }

    public required string Code { get; set; }
    public required string Title { get; set; }
    public bool IsEligible { get; set; }
}
