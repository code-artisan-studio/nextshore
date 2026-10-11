namespace NextShore.Domain.Entities;

public class Country : RuleEntity
{
    /// <summary>ISO 3166-1 alpha-2, e.g. <c>CA</c>.</summary>
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary><c>NOC</c> · <c>ISCO</c> · <c>ANZSCO</c>.</summary>
    public required string OccupationSystem { get; set; }

    /// <summary><c>CLB</c> · <c>CEFR</c> · <c>IELTS</c>.</summary>
    public required string LanguageScale { get; set; }

    public List<Pathway> Pathways { get; set; } = [];
}
