using NextShore.Domain.Entities;

namespace NextShore.Domain.Tests.Entities;

public class SourcedRuleEntityTests
{
    private static Requirement NewRequirement() => new()
    {
        Field = "work.years_skilled",
        FixHint = "Gain more skilled experience",
        Label = "Skilled work experience",
        SourceUrl = "https://example.gov/rule",
    };

    [Fact]
    public void Is_publishable_when_verified_and_no_unverified_fields()
    {
        var requirement = NewRequirement();
        requirement.VerifiedAt = new DateOnly(2026, 10, 1);

        Assert.True(requirement.IsPublishable);
    }

    [Fact]
    public void Is_not_publishable_while_a_field_is_unverified()
    {
        var requirement = NewRequirement();
        requirement.VerifiedAt = new DateOnly(2026, 10, 1);
        requirement.UnverifiedFields.Add("value");

        Assert.False(requirement.IsPublishable);
    }

    [Fact]
    public void Is_not_publishable_without_verified_date()
    {
        Assert.False(NewRequirement().IsPublishable);
    }
}
