namespace NextShore.Domain.Engine;

/// <summary>One normalised profile value, compared against rule values by the engine.</summary>
public abstract record ProfileValue
{
    public sealed record Number(decimal Value) : ProfileValue;

    public sealed record Text(string Value) : ProfileValue;

    public sealed record Flag(bool Value) : ProfileValue;

    public sealed record Money(decimal Amount, string Currency) : ProfileValue;

    /// <summary>A level on an ordered scale, e.g. CEFR B1 or education <c>masters</c>.</summary>
    public sealed record Ordinal(OrdinalScale Scale, int Rank) : ProfileValue
    {
        public string Label => Scale.Labels[Rank];
    }

    /// <summary>Scores below the lowest level a mapping defines, e.g. "Below CLB 4". Less than any level.</summary>
    public sealed record Below(string Scale, string LowestLabel) : ProfileValue;
}

public sealed record OrdinalScale(string Name, IReadOnlyList<string> Labels)
{
    public static OrdinalScale Cefr { get; } = new("CEFR", ["A1", "A2", "B1", "B2", "C1", "C2"]);

    public static OrdinalScale EducationLevel { get; } =
        new("education", ["secondary", "diploma", "bachelors", "masters", "phd"]);

    private static readonly OrdinalScale[] Known = [Cefr, EducationLevel];

    public int? RankOf(string label)
    {
        for (var i = 0; i < Labels.Count; i++)
        {
            if (string.Equals(Labels[i], label, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>The ordered scale with this name, or null for numeric scales such as CLB.</summary>
    public static OrdinalScale? Find(string name) =>
        Array.Find(Known, s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
}
