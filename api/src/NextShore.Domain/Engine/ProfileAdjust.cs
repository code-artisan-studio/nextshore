namespace NextShore.Domain.Engine;

/// <summary>
/// Changes one profile value the way a real fix would. Language levels stay consistent: the overall
/// <c>{lang}.level.{scale}</c> is the lowest of its per-skill keys, so raising one moves the other.
/// </summary>
public static class ProfileAdjust
{
    private static readonly string[] Skills = ["listening", "reading", "writing", "speaking"];

    public static NormalizedProfile Set(NormalizedProfile profile, string key, ProfileValue value)
    {
        var changed = profile.With(key, value);

        if (SkillsOf(changed, key) is { } skillKeys)
        {
            // Overall level raised: every skill below it rises to it.
            foreach (var skillKey in skillKeys.Where(k => Order(changed.Get(k)!) < Order(value)))
            {
                changed = changed.With(skillKey, value);
            }

            return changed;
        }

        var lastDot = key.LastIndexOf('.');
        var overall = lastDot > 0 ? key[..lastDot] : null;
        if (overall is not null && Skills.Contains(key[(lastDot + 1)..]) && changed.Get(overall) is not null
            && SkillsOf(changed, overall) is { Count: 4 } all)
        {
            // Skill raised: overall is recomputed as the lowest skill.
            changed = changed.With(overall, all.Select(k => changed.Get(k)!).MinBy(Order));
        }

        return changed;
    }

    private static List<string>? SkillsOf(NormalizedProfile profile, string overallKey)
    {
        if (!overallKey.Contains(".level.", StringComparison.Ordinal))
        {
            return null;
        }

        var keys = Skills.Select(s => $"{overallKey}.{s}").Where(k => profile.Get(k) is not null).ToList();
        return keys.Count == 0 ? null : keys;
    }

    /// <summary>Comparable order for level-like values; text and flags do not order.</summary>
    internal static decimal Order(ProfileValue value) => value switch
    {
        ProfileValue.Number n => n.Value,
        ProfileValue.Ordinal o => o.Rank,
        ProfileValue.Below => decimal.MinValue,
        _ => 0,
    };
}
