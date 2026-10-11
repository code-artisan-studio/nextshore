namespace NextShore.Domain.Engine;

/// <summary>Flat, immutable map of profile field keys (DATA-MODEL.md) to values. Missing key = not provided.</summary>
public sealed class NormalizedProfile(IReadOnlyDictionary<string, ProfileValue> values)
{
    public IReadOnlyDictionary<string, ProfileValue> Values { get; } = values;

    public ProfileValue? Get(string key) => Values.GetValueOrDefault(key);

    /// <summary>A copy with <paramref name="key"/> set, or removed when <paramref name="value"/> is null.</summary>
    public NormalizedProfile With(string key, ProfileValue? value)
    {
        var values = new Dictionary<string, ProfileValue>(Values);
        if (value is null)
        {
            values.Remove(key);
        }
        else
        {
            values[key] = value;
        }

        return new NormalizedProfile(values);
    }
}
