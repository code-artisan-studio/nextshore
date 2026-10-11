using System.Net.Http.Json;
using NextShore.Application.Countries;
using NextShore.Application.Occupations;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class RulesEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Countries_lists_each_country_with_its_published_pathways()
    {
        var countries = await factory.CreateClient().GetFromJsonAsync<List<CountryResponse>>("/api/v1/countries") ?? [];

        Assert.Equal(["AU", "CA", "DE"], countries.Select(c => c.Code));
        var canada = countries.Single(c => c.Code == "CA");
        Assert.Equal("Canada", canada.Name);
        Assert.Equal(["ca-express-entry", "ca-pgwp", "ca-study-permit"], canada.Pathways.Select(p => p.Slug));
        var pgwp = canada.Pathways.Single(p => p.Slug == "ca-pgwp");
        Assert.Equal("post_study_work", pgwp.Type);
        Assert.False(pgwp.IsPointsBased);
        Assert.Equal(["au-189"], countries.Single(c => c.Code == "AU").Pathways.Select(p => p.Slug)); // 500 & 485 unverified
    }

    [Theory]
    [InlineData("212", null, new[] { "21211", "21231", "21232", "21234" })] // code prefix
    [InlineData("software", "NOC", new[] { "21231", "21232" })]             // title, case-insensitive
    [InlineData("software", "noc", new[] { "21231", "21232" })]
    [InlineData("software", "ANZSCO", new string[0])]                        // unverified entries are not published
    [InlineData("zzz", null, new string[0])]
    public async Task Occupations_match_code_prefix_or_title(string q, string? system, string[] expected)
    {
        var url = $"/api/v1/occupations?q={q}" + (system is null ? "" : $"&system={system}");

        var occupations = await factory.CreateClient().GetFromJsonAsync<List<OccupationResponse>>(url) ?? [];

        Assert.Equal(expected, occupations.Select(o => o.Code));
        Assert.All(occupations, o => Assert.Equal("NOC", o.System));
    }

    [Fact]
    public async Task Occupations_rank_code_matches_first_and_return_at_most_20()
    {
        var source = new RuleSource("https://example.gov", new DateOnly(2026, 10, 10));
        var occupations = Enumerable.Range(0, 30)
            .Select(i => new OccupationRule($"9{i:00}", $"Title {i}", true, source))
            .Append(new OccupationRule("100", "Analyst 9", true, source))
            .ToList();
        var snapshot = new RuleSnapshot(1, [new CountryRule("XX", "Testland", "NOC", "CLB")],
        [
            new PathwayRule("xx-pr", "XX", "PR", PathwayType.Pr, [Audience.Developer], false, null, null, null, null, null,
                "Test", 1, source, [], [], occupations),
        ], []);
        using var custom = new ApiFactory { Snapshot = snapshot };

        var result = await custom.CreateClient().GetFromJsonAsync<List<OccupationResponse>>("/api/v1/occupations?q=9");

        Assert.Equal(20, result!.Count);
        Assert.All(result, o => Assert.StartsWith("9", o.Code)); // "100 Analyst 9" (title match) ranks after code matches
    }
}
