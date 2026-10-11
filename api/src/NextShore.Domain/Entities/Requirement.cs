using NextShore.Domain.Enums;

namespace NextShore.Domain.Entities;

public class Requirement : SourcedRuleEntity
{
    public Guid PathwayId { get; set; }
    public Pathway? Pathway { get; set; }

    /// <summary>Profile field key, e.g. <c>english.level.CLB</c> (see DATA-MODEL.md).</summary>
    public required string Field { get; set; }
    public RequirementOperator Operator { get; set; }

    /// <summary>Scalar or array as raw JSON, e.g. <c>7</c> or <c>["bachelors","masters"]</c>.</summary>
    public string? Value { get; set; }
    public string? Unit { get; set; }

    /// <summary>False = advisory.</summary>
    public bool IsMandatory { get; set; }
    public Fixability Fixability { get; set; }
    public required string FixHint { get; set; }
    public int? FixMonths { get; set; }
    public required string Label { get; set; }
}
