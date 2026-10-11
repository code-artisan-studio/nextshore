using NextShore.Domain.Engine;
using NextShore.Domain.Enums;
using static NextShore.Domain.Tests.Engine.TestRules;

namespace NextShore.Domain.Tests.Engine;

public class GraderTests
{
    private static RequirementOutcome Req(bool met, Fixability fixability = Fixability.Medium, bool mandatory = true) =>
        new(Guid.NewGuid(), "Requirement", met, "current", "target", mandatory, fixability, "Fix it", 3, Source);

    private static RequirementOutcome[] Reqs(int met, int unmet) =>
        [.. Enumerable.Range(0, met).Select(_ => Req(true)), .. Enumerable.Range(0, unmet).Select(_ => Req(false))];

    private static PointsBreakdown Points(int total, int? target) =>
        new(PointsVariant.Single, total, target, target is null ? null : PointsTargetKind.Cutoff, []);

    private static GradeResult Grade(IReadOnlyList<RequirementOutcome> requirements, PointsBreakdown? points = null,
        GraderOptions? options = null) =>
        Grader.Evaluate(requirements, points, options ?? GraderOptions.Default);

    [Fact]
    public void All_requirements_met_without_points_is_eligible_with_100()
    {
        Assert.Equal(new GradeResult(Enums.Grade.Eligible, 100), Grade(Reqs(3, 0)));
    }

    [Theory]
    [InlineData(480, Enums.Grade.Eligible)]      // total = target
    [InlineData(479, Enums.Grade.NearEligible)]  // just short
    [InlineData(600, Enums.Grade.Eligible)]
    public void Points_must_reach_the_target_to_be_eligible(int total, Grade expected)
    {
        Assert.Equal(expected, Grade(Reqs(4, 0), Points(total, 480)).Grade);
    }

    [Theory]
    [InlineData(408, Enums.Grade.NearEligible)] // exactly 85% of 480
    [InlineData(407, Enums.Grade.NotEligible)]
    public void Near_eligible_needs_85_percent_of_the_target(int total, Grade expected)
    {
        Assert.Equal(expected, Grade(Reqs(4, 0), Points(total, 480)).Grade);
    }

    [Theory]
    [InlineData(1, Enums.Grade.NearEligible)]
    [InlineData(2, Enums.Grade.NearEligible)]
    [InlineData(3, Enums.Grade.NotEligible)]
    public void Near_eligible_allows_at_most_two_unmet_requirements(int unmet, Grade expected)
    {
        Assert.Equal(expected, Grade(Reqs(5, unmet)).Grade);
    }

    [Fact]
    public void Near_eligible_needs_both_few_gaps_and_enough_points()
    {
        Assert.Equal(Enums.Grade.NotEligible, Grade(Reqs(4, 1), Points(400, 480)).Grade);
        Assert.Equal(Enums.Grade.NearEligible, Grade(Reqs(4, 1), Points(470, 480)).Grade);
    }

    [Fact]
    public void Unmet_impossible_requirement_caps_the_score_and_blocks_near_eligible()
    {
        RequirementOutcome[] requirements = [.. Reqs(4, 0), Req(false, Fixability.Impossible)];

        var result = Grade(requirements);

        Assert.Equal(Enums.Grade.NotEligible, result.Grade);
        Assert.Equal(20, result.ReadinessScore); // would be round(60 × 4/5 + 40) = 88
    }

    [Fact]
    public void Met_impossible_requirement_has_no_effect()
    {
        Assert.Equal(new GradeResult(Enums.Grade.Eligible, 100), Grade([Req(true, Fixability.Impossible), Req(true)]));
    }

    [Fact]
    public void Low_score_is_not_raised_by_the_impossible_cap()
    {
        RequirementOutcome[] requirements = [Req(false, Fixability.Impossible), .. Reqs(0, 4)];

        Assert.Equal(new GradeResult(Enums.Grade.NotEligible, 0), Grade(requirements, Points(0, 100)));
    }

    [Fact]
    public void Advisory_requirements_do_not_count()
    {
        RequirementOutcome[] requirements = [.. Reqs(2, 0), Req(false, Fixability.Impossible, mandatory: false)];

        Assert.Equal(new GradeResult(Enums.Grade.Eligible, 100), Grade(requirements));
    }

    [Fact]
    public void Score_is_60_for_requirements_plus_40_for_points()
    {
        // 60 × 3/4 + 40 × 455/480 = 45 + 37.92 = 82.92
        Assert.Equal(83, Grade(Reqs(3, 1), Points(455, 480)).ReadinessScore);
    }

    [Fact]
    public void Score_rounds_half_away_from_zero()
    {
        // 60 × 1/8 + 40 = 47.5
        Assert.Equal(48, Grade(Reqs(1, 7)).ReadinessScore);
    }

    [Fact]
    public void Points_above_the_target_count_as_full()
    {
        Assert.Equal(100, Grade(Reqs(2, 0), Points(900, 480)).ReadinessScore);
    }

    [Fact]
    public void No_mandatory_requirements_counts_as_all_met()
    {
        Assert.Equal(new GradeResult(Enums.Grade.Eligible, 100), Grade([]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Points_without_a_usable_target_do_not_block(int? target)
    {
        Assert.Equal(new GradeResult(Enums.Grade.Eligible, 100), Grade(Reqs(2, 0), Points(300, target)));
    }

    [Fact]
    public void Thresholds_come_from_options()
    {
        var strict = new GraderOptions(NearEligibleMaxGaps: 1, NearEligiblePointsRatio: 0.95m);

        Assert.Equal(Enums.Grade.NotEligible, Grade(Reqs(5, 2), options: strict).Grade);
        Assert.Equal(Enums.Grade.NotEligible, Grade(Reqs(4, 0), Points(455, 480), strict).Grade); // 94.8%
        Assert.Equal(Enums.Grade.NearEligible, Grade(Reqs(4, 0), Points(456, 480), strict).Grade); // 95%
    }
}
