using NextShore.Domain.Entities;
using NextShore.Domain.Rules;

namespace NextShore.Domain.Tests.Rules;

public class PublishGuardTests
{
    private static readonly DateOnly Verified = new(2026, 10, 1);

    private static Pathway VerifiedPathway() => new()
    {
        Slug = "xx-test",
        Name = "Test pathway",
        Summary = "Test",
        SourceUrl = "https://example.gov",
        VerifiedAt = Verified,
        Requirements =
        [
            new Requirement
            {
                Field = "age",
                FixHint = "n/a",
                Label = "Age limit",
                SourceUrl = "https://example.gov",
                VerifiedAt = Verified,
            },
        ],
    };

    [Fact]
    public void Fully_verified_pathway_has_no_blockers()
    {
        Assert.Empty(PublishGuard.Check(VerifiedPathway()));
    }

    [Fact]
    public void Todo_verify_on_a_child_row_blocks_publish()
    {
        var pathway = VerifiedPathway();
        pathway.Requirements[0].UnverifiedFields.Add("value");

        var blocker = Assert.Single(PublishGuard.Check(pathway));

        Assert.Equal("requirement", blocker.Entity);
        Assert.Equal("Age limit", blocker.Label);
        Assert.Equal("TODO_VERIFY: value", blocker.Reason);
    }

    [Fact]
    public void Missing_verified_date_blocks_publish()
    {
        var pathway = VerifiedPathway();
        pathway.VerifiedAt = null;

        var blocker = Assert.Single(PublishGuard.Check(pathway));

        Assert.Equal("pathway", blocker.Entity);
        Assert.Equal("verified_at not set", blocker.Reason);
    }
}
