using NextShore.Domain.Entities;

namespace NextShore.Domain.Rules;

public sealed record PublishBlocker(string Entity, string Label, string Reason);

/// <summary>A rule can be published only once every value is verified against its source.</summary>
public static class PublishGuard
{
    public static IReadOnlyList<PublishBlocker> Check(Pathway pathway)
    {
        var blockers = new List<PublishBlocker>();
        AddBlockers(blockers, "pathway", pathway.Slug, pathway);
        foreach (var requirement in pathway.Requirements)
        {
            AddBlockers(blockers, "requirement", requirement.Label, requirement);
        }

        foreach (var factor in pathway.PointsFactors)
        {
            AddBlockers(blockers, "points_factor", $"{factor.Factor} ({factor.Variant})", factor);
        }

        foreach (var occupation in pathway.Occupations)
        {
            AddBlockers(blockers, "occupation", occupation.Code, occupation);
        }

        return blockers;
    }

    public static IReadOnlyList<PublishBlocker> Check(LanguageScoreMapping mapping)
    {
        var blockers = new List<PublishBlocker>();
        AddBlockers(blockers, "language_score_mapping",
            $"{mapping.Test}{(mapping.TestVariant is null ? "" : $" ({mapping.TestVariant})")} {mapping.Skill} → " +
            $"{mapping.TargetScale} {mapping.TargetLevel}", mapping);
        return blockers;
    }

    private static void AddBlockers(List<PublishBlocker> blockers, string entity, string label, SourcedRuleEntity rule)
    {
        if (rule.UnverifiedFields.Count > 0)
        {
            blockers.Add(new(entity, label, $"{RuleValues.TodoVerify}: {string.Join(", ", rule.UnverifiedFields)}"));
        }

        if (rule.VerifiedAt is null)
        {
            blockers.Add(new(entity, label, "verified_at not set"));
        }
    }
}
