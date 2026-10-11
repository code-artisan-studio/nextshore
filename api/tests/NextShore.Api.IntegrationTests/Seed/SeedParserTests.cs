using NextShore.Domain.Enums;
using NextShore.Infrastructure.Seed;

namespace NextShore.Api.IntegrationTests.Seed;

public class SeedParserTests
{
    private const string CountryJson = """
        {
          "country": { "code": "ca", "name": "Canada", "occupationSystem": "NOC", "languageScale": "CLB" },
          "pathways": [
            {
              "slug": "ca-express-entry",
              "name": "Express Entry",
              "type": "pr",
              "audience": ["developer", "student"],
              "isPointsBased": true,
              "summary": "Points-ranked PR",
              "sourceUrl": "https://example.gov/ee",
              "passMark": null,
              "cutoff": "TODO_VERIFY",
              "cutoffAsOf": "TODO_VERIFY",
              "nextPathwaySlug": null,
              "typicalDurationMonths": 6,
              "requirements": [
                { "field": "english.level.CLB", "operator": "gte", "value": "TODO_VERIFY", "unit": "CLB",
                  "isMandatory": true, "fixability": "easy", "fixHint": "Retake test", "fixMonths": 2, "label": "English" },
                { "field": "education.level", "operator": "in", "value": ["bachelors", "masters"],
                  "isMandatory": true, "fixability": "hard", "fixHint": "Study", "label": "Degree",
                  "sourceUrl": "https://example.gov/edu" }
              ],
              "pointsFactors": [
                { "factor": "age", "variant": "with_spouse", "min": 20, "max": "TODO_VERIFY", "points": "TODO_VERIFY" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Todo_verify_values_become_null_and_are_listed_as_unverified()
    {
        var pathway = Assert.Single(SeedParser.ParseCountryFile(CountryJson, "ca.json").Pathways).Pathway;

        Assert.Null(pathway.Cutoff);
        Assert.Null(pathway.CutoffAsOf);
        Assert.Equal(6, pathway.TypicalDurationMonths);
        Assert.Equal(["cutoff", "cutoff_as_of"], pathway.UnverifiedFields);

        Assert.Null(pathway.Requirements[0].Value);
        Assert.Equal(["value"], pathway.Requirements[0].UnverifiedFields);

        var factor = Assert.Single(pathway.PointsFactors);
        Assert.Equal(PointsVariant.WithSpouse, factor.Variant);
        Assert.Equal(20m, factor.Min);
        Assert.Equal(["max", "points"], factor.UnverifiedFields);
    }

    [Fact]
    public void Verified_values_are_kept_and_children_inherit_the_pathway_source()
    {
        var parsed = SeedParser.ParseCountryFile(CountryJson, "ca.json");
        var pathway = parsed.Pathways[0].Pathway;

        Assert.Equal("CA", parsed.Country.Code);
        Assert.Equal(RuleStatus.Draft, pathway.Status);
        Assert.Equal([Audience.Developer, Audience.Student], pathway.Audience);
        Assert.Equal("""["bachelors", "masters"]""", pathway.Requirements[1].Value);
        Assert.Empty(pathway.Requirements[1].UnverifiedFields);
        Assert.Equal("https://example.gov/ee", pathway.Requirements[0].SourceUrl);
        Assert.Equal("https://example.gov/edu", pathway.Requirements[1].SourceUrl);
    }

    [Fact]
    public void Verified_at_is_never_inherited_from_the_pathway()
    {
        var json = CountryJson.Replace("\"sourceUrl\": \"https://example.gov/ee\",",
            "\"sourceUrl\": \"https://example.gov/ee\", \"verifiedAt\": \"2026-10-10\",");

        var pathway = SeedParser.ParseCountryFile(json, "ca.json").Pathways[0].Pathway;

        Assert.Equal(new DateOnly(2026, 10, 10), pathway.VerifiedAt);
        Assert.All(pathway.Requirements, r => Assert.Null(r.VerifiedAt));
        Assert.All(pathway.PointsFactors, f => Assert.Null(f.VerifiedAt));
    }

    [Theory]
    [InlineData("\"operator\": \"gte\"", "\"operator\": \"greater\"", "operator")]
    [InlineData("\"typicalDurationMonths\": 6", "\"typicalDurationMonths\": \"six\"", "typical_duration_months")]
    [InlineData("\"summary\": \"Points-ranked PR\",", "\"summry\": \"typo\",", "summry")]
    public void Malformed_files_fail_with_the_offending_path(string find, string replace, string expectedInMessage)
    {
        var json = CountryJson.Replace(find, replace);

        var ex = Assert.Throws<SeedException>(() => SeedParser.ParseCountryFile(json, "ca.json"));

        Assert.StartsWith("ca.json", ex.Message);
        Assert.Contains(expectedInMessage, ex.Message);
    }

    [Fact]
    public void Language_mappings_expand_to_one_row_per_skill()
    {
        const string json = """
            {
              "groups": [
                {
                  "test": "ielts", "testVariant": "general", "targetScale": "CLB", "sourceUrl": "https://example.gov/clb",
                  "levels": [
                    { "targetLevel": "7",
                      "minScores": { "listening": 6.0, "reading": "TODO_VERIFY", "writing": 6.0, "speaking": 6.0 } }
                  ]
                }
              ]
            }
            """;

        var mappings = SeedParser.ParseLanguageMappings(json, "language-mappings.json");

        Assert.Equal(4, mappings.Count);
        Assert.All(mappings, m => Assert.Equal("general", m.TestVariant));
        var reading = Assert.Single(mappings, m => m.Skill == "reading");
        Assert.Null(reading.MinScore);
        Assert.Equal(["min_score"], reading.UnverifiedFields);
        Assert.Equal(6.0m, mappings.Single(m => m.Skill == "listening").MinScore);
    }
}
