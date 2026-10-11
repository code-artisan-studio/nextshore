using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Rules;
using NextShore.Infrastructure.Seed;

namespace NextShore.Api.IntegrationTests.Engine;

/// <summary>
/// The real api/seed rules as if every verified row were published: what the engine will compute once they are.
/// Pathways never verified (au-student-500, au-485) and rows still TODO_VERIFY are left out; pathway values that
/// are still TODO_VERIFY (e.g. some durations) stay unknown.
/// </summary>
internal static class VerifiedSeedSnapshot
{
    public static RuleSnapshot Load()
    {
        var directory = FindSeedDirectory();
        var countries = new List<Country>();
        var pathways = new List<(Pathway Pathway, string? Next)>();
        foreach (var file in Directory.GetFiles(directory, "*.json").Where(f => Path.GetFileName(f) != SeedParser.LanguageMappingsFileName))
        {
            var parsed = SeedParser.ParseCountryFile(File.ReadAllText(file), Path.GetFileName(file));
            parsed.Country.Id = Guid.NewGuid();
            countries.Add(parsed.Country);
            foreach (var seeded in parsed.Pathways.Where(p => p.Pathway.VerifiedAt is not null))
            {
                var pathway = seeded.Pathway;
                pathway.Id = Guid.NewGuid();
                pathway.CountryId = parsed.Country.Id;
                pathway.Status = RuleStatus.Published;
                pathway.RuleVersion = 1;
                pathway.UnverifiedFields.Clear(); // their values are null = unknown
                foreach (var row in pathway.Requirements.Cast<SourcedRuleEntity>().Concat(pathway.PointsFactors).Concat(pathway.Occupations))
                {
                    row.Id = Guid.NewGuid();
                    row.Status = row.IsPublishable ? RuleStatus.Published : RuleStatus.Draft;
                }

                pathways.Add((pathway, seeded.NextPathwaySlug));
            }
        }

        var bySlug = pathways.ToDictionary(p => p.Pathway.Slug, p => p.Pathway);
        foreach (var (pathway, next) in pathways)
        {
            pathway.NextPathway = next is null ? null : bySlug.GetValueOrDefault(next);
        }

        var mappings = SeedParser.ParseLanguageMappings(
            File.ReadAllText(Path.Combine(directory, SeedParser.LanguageMappingsFileName)), SeedParser.LanguageMappingsFileName);
        foreach (var mapping in mappings)
        {
            mapping.Status = mapping.IsPublishable ? RuleStatus.Published : RuleStatus.Draft;
        }

        return RuleSnapshotMapper.Map(countries, pathways.Select(p => p.Pathway).ToList(), mappings);
    }

    private static string FindSeedDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NextShore.sln")))
            {
                return Path.Combine(dir.FullName, "seed");
            }
        }

        throw new DirectoryNotFoundException("api/seed not found above the test output directory.");
    }
}
