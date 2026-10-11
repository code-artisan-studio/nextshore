using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Seed;

namespace NextShore.Infrastructure.Rules;

/// <summary>
/// Development only (<c>Rules:Source = seed-preview</c>): serves the api/seed rules as if every verified row were
/// published, so the web app has data before the admin publish flow exists. Pathways never verified and rows still
/// TODO_VERIFY are left out; pathway values still TODO_VERIFY stay unknown (null).
/// </summary>
public sealed class SeedPreviewSnapshotSource(string seedDirectory) : IRuleSnapshotSource
{
    public Task<RuleSnapshot> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(Build(seedDirectory));

    public static RuleSnapshot Build(string seedDirectory)
    {
        var countries = new List<Country>();
        var pathways = new List<(Pathway Pathway, string? Next)>();
        foreach (var file in Directory.GetFiles(seedDirectory, "*.json")
                     .Where(f => Path.GetFileName(f) != SeedParser.LanguageMappingsFileName)
                     .Order(StringComparer.Ordinal))
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

        var mappingsFile = Path.Combine(seedDirectory, SeedParser.LanguageMappingsFileName);
        var mappings = File.Exists(mappingsFile)
            ? SeedParser.ParseLanguageMappings(File.ReadAllText(mappingsFile), SeedParser.LanguageMappingsFileName)
            : [];
        foreach (var mapping in mappings)
        {
            mapping.Status = mapping.IsPublishable ? RuleStatus.Published : RuleStatus.Draft;
        }

        return RuleSnapshotMapper.Map(countries, pathways.Select(p => p.Pathway).ToList(), mappings);
    }
}
