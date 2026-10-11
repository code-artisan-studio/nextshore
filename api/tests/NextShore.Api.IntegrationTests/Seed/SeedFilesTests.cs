using NextShore.Domain.Entities;
using NextShore.Infrastructure.Seed;

namespace NextShore.Api.IntegrationTests.Seed;

/// <summary>Guards the real files in api/seed/.</summary>
public class SeedFilesTests
{
    private static readonly string SeedDirectory = SeedPaths.Directory;

    private static IReadOnlyList<SeededPathway> AllPathways() =>
        Directory.GetFiles(SeedDirectory, "*.json")
            .Where(f => Path.GetFileName(f) != SeedParser.LanguageMappingsFileName)
            .SelectMany(f => SeedParser.ParseCountryFile(File.ReadAllText(f), Path.GetFileName(f)).Pathways)
            .ToList();

    [Fact]
    public void Seeds_the_ten_poc_pathways_with_their_chains()
    {
        var chains = AllPathways().ToDictionary(p => p.Pathway.Slug, p => p.NextPathwaySlug);

        // VISA-RULES-SPEC.md §8
        Assert.Equal(new Dictionary<string, string?>
        {
            ["ca-study-permit"] = "ca-pgwp",
            ["ca-pgwp"] = "ca-express-entry",
            ["ca-express-entry"] = null,
            ["de-student-visa"] = "de-post-study",
            ["de-post-study"] = "de-blue-card",
            ["de-blue-card"] = null,
            ["de-opportunity-card"] = "de-blue-card",
            ["au-student-500"] = "au-485",
            ["au-485"] = "au-189",
            ["au-189"] = null,
        }, chains);
    }

    /// <summary>CLAUDE.md rule 5: never invent visa values. A value may only be set on a row with verified_at.</summary>
    [Fact]
    public void Unverified_rows_carry_no_visa_values()
    {
        var invented = new List<string>();
        foreach (var pathway in AllPathways().Select(p => p.Pathway))
        {
            if (pathway.VerifiedAt is null
                && (pathway.PassMark ?? pathway.Cutoff ?? pathway.TypicalDurationMonths) is not null)
            {
                invented.Add(pathway.Slug);
            }

            invented.AddRange(pathway.Requirements
                .Where(r => r.VerifiedAt is null && r.Value is not null)
                .Select(r => $"{pathway.Slug}: requirement '{r.Label}'"));
            invented.AddRange(pathway.PointsFactors
                .Where(f => f.VerifiedAt is null
                    && (f.Min is not null || f.Max is not null || f.MatchValue is not null
                        || f.Points is not null || f.MaxPointsForFactor is not null))
                .Select(f => $"{pathway.Slug}: points factor '{f.Factor}'"));
            invented.AddRange(pathway.Occupations
                .Where(o => o.VerifiedAt is null && !o.UnverifiedFields.Contains("is_eligible"))
                .Select(o => $"{pathway.Slug}: occupation {o.Code}"));
        }

        var mappings = SeedParser.ParseLanguageMappings(
            File.ReadAllText(Path.Combine(SeedDirectory, SeedParser.LanguageMappingsFileName)),
            SeedParser.LanguageMappingsFileName);
        invented.AddRange(mappings
            .Where(m => m.VerifiedAt is null && m.MinScore is not null)
            .Select(m => $"{m.Test} {m.Skill} → {m.TargetScale} {m.TargetLevel}"));

        Assert.Empty(invented);
    }

    [Fact]
    public void Every_rule_row_has_an_https_source()
    {
        var rows = AllPathways().SelectMany(p => new SourcedRuleEntity[] { p.Pathway }
            .Concat(p.Pathway.Requirements)
            .Concat(p.Pathway.PointsFactors)
            .Concat(p.Pathway.Occupations));

        Assert.All(rows, row => Assert.StartsWith("https://", row.SourceUrl));
    }
}
