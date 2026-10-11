using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Infrastructure.Rules;

namespace NextShore.Api.IntegrationTests.Rules;

public class RuleSnapshotMapperTests
{
    private static readonly DateOnly Verified = new(2026, 10, 10);
    private static readonly Country Canada =
        new() { Id = Guid.NewGuid(), Code = "CA", Name = "Canada", OccupationSystem = "NOC", LanguageScale = "CLB" };

    private static Pathway NewPathway(string slug, RuleStatus status, int ruleVersion = 1) => new()
    {
        Id = Guid.NewGuid(),
        CountryId = Canada.Id,
        Slug = slug,
        Name = slug,
        Summary = slug,
        Status = status,
        RuleVersion = ruleVersion,
        SourceUrl = "https://example.gov",
        VerifiedAt = Verified,
    };

    private static Requirement NewRequirement(string label, RuleStatus status) => new()
    {
        Field = "english.level.CLB",
        Operator = RequirementOperator.Gte,
        Value = "7",
        FixHint = "Retake test",
        Label = label,
        Status = status,
        SourceUrl = "https://example.gov",
        VerifiedAt = Verified,
    };

    [Fact]
    public void Includes_only_published_pathways_and_rows()
    {
        var published = NewPathway("ca-pgwp", RuleStatus.Published, ruleVersion: 3);
        published.Requirements = [NewRequirement("live", RuleStatus.Published), NewRequirement("draft", RuleStatus.Draft)];
        var draft = NewPathway("ca-study-permit", RuleStatus.Draft);

        var snapshot = RuleSnapshotMapper.Map([Canada], [published, draft], []);

        var pathway = Assert.Single(snapshot.Pathways);
        Assert.Equal("ca-pgwp", pathway.Slug);
        Assert.Equal("CA", pathway.CountryCode);
        Assert.Equal("live", Assert.Single(pathway.Requirements).Label);
        Assert.Equal(3, snapshot.Version);
        Assert.Same(pathway, snapshot.FindPathway("ca-pgwp"));
        Assert.Null(snapshot.FindPathway("ca-study-permit"));
    }

    [Fact]
    public void Chain_stops_at_a_pathway_that_is_not_published()
    {
        var study = NewPathway("ca-study-permit", RuleStatus.Published);
        var pgwp = NewPathway("ca-pgwp", RuleStatus.Published);
        var expressEntry = NewPathway("ca-express-entry", RuleStatus.Draft);
        study.NextPathway = pgwp;
        pgwp.NextPathway = expressEntry;

        var snapshot = RuleSnapshotMapper.Map([Canada], [study, pgwp, expressEntry], []);

        Assert.Equal("ca-pgwp", snapshot.FindPathway("ca-study-permit")!.NextPathwaySlug);
        Assert.Null(snapshot.FindPathway("ca-pgwp")!.NextPathwaySlug);
    }

    [Fact]
    public void Fails_loudly_on_a_published_row_that_is_not_verified()
    {
        var pathway = NewPathway("ca-pgwp", RuleStatus.Published);
        var requirement = NewRequirement("English", RuleStatus.Published);
        requirement.UnverifiedFields.Add("value");
        pathway.Requirements = [requirement];

        var ex = Assert.Throws<InvalidOperationException>(() => RuleSnapshotMapper.Map([Canada], [pathway], []));

        Assert.Contains("ca-pgwp requirement 'English'", ex.Message);
        Assert.Contains("TODO_VERIFY: value", ex.Message);
    }

    [Fact]
    public void Nothing_published_gives_an_empty_snapshot_with_countries()
    {
        var snapshot = RuleSnapshotMapper.Map([Canada], [NewPathway("ca-pgwp", RuleStatus.Draft)], []);

        Assert.Equal(0, snapshot.Version);
        Assert.Empty(snapshot.Pathways);
        Assert.Equal("CA", Assert.Single(snapshot.Countries).Code);
    }
}
