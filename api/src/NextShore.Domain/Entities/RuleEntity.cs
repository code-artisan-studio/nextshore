using NextShore.Domain.Enums;

namespace NextShore.Domain.Entities;

/// <summary>Base for every admin-editable rule row (draft → published → archived).</summary>
public abstract class RuleEntity
{
    public Guid Id { get; set; }
    public RuleStatus Status { get; set; } = RuleStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// A rule backed by an official source. Values not yet checked against the source are listed in
/// <see cref="UnverifiedFields"/> (seeded as <c>TODO_VERIFY</c>) and left null until verified.
/// </summary>
public abstract class SourcedRuleEntity : RuleEntity
{
    public required string SourceUrl { get; set; }
    public DateOnly? VerifiedAt { get; set; }
    public List<string> UnverifiedFields { get; set; } = [];

    public bool IsPublishable => UnverifiedFields.Count == 0 && VerifiedAt is not null;
}
