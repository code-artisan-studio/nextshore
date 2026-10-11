using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextShore.Domain.Entities;
using NextShore.Domain.Enums;
using NextShore.Domain.Rules;
using NextShore.Infrastructure.Persistence;

namespace NextShore.Infrastructure.Seed;

public enum SeedOutcome
{
    Created,
    Updated,
    SkippedPublished,
}

public sealed record PathwaySeedResult(
    string Slug,
    SeedOutcome Outcome,
    int Requirements,
    int PointsFactors,
    int Occupations,
    int UnverifiedValues,
    int PublishBlockers);

public sealed record SeedReport(
    IReadOnlyList<PathwaySeedResult> Pathways,
    int LanguageMappingsCreated,
    int LanguageMappingsUpdated,
    int LanguageMappingsSkippedPublished,
    int LanguageMappingsUnverified);

/// <summary>
/// Loads <c>api/seed/*.json</c> as drafts in one transaction. Countries upsert by code, pathways by slug,
/// language mappings by (test, skill, target scale, level). Draft pathways have their child rows replaced;
/// anything already published is left alone, because the admin screen owns it from then on.
/// </summary>
public sealed class SeedLoader(NextShoreDbContext db, ILogger<SeedLoader> logger)
{
    public async Task<SeedReport> LoadAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
        {
            throw new SeedException($"Seed directory not found: {directory}");
        }

        var files = Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal).ToList();
        var countryFiles = files.Where(f => Path.GetFileName(f) != SeedParser.LanguageMappingsFileName).ToList();
        var mappingsFile = files.FirstOrDefault(f => Path.GetFileName(f) == SeedParser.LanguageMappingsFileName);
        logger.LogInformation("Seeding from {Directory}: {Count} file(s)", directory, files.Count);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var results = new List<PathwaySeedResult>();
        var links = new List<(Pathway Pathway, string? NextSlug)>();
        foreach (var file in countryFiles)
        {
            var parsed = SeedParser.ParseCountryFile(
                await File.ReadAllTextAsync(file, cancellationToken), Path.GetFileName(file));
            var country = await UpsertCountryAsync(parsed.Country, cancellationToken);
            logger.LogInformation("{File} → {Code} {Name}", Path.GetFileName(file), country.Code, country.Name);

            foreach (var seeded in parsed.Pathways)
            {
                var (pathway, outcome) = await UpsertPathwayAsync(country, seeded.Pathway, cancellationToken);
                if (outcome != SeedOutcome.SkippedPublished)
                {
                    links.Add((pathway, seeded.NextPathwaySlug));
                }

                var result = Summarise(pathway, outcome);
                results.Add(result);
                logger.LogInformation(
                    "  {Slug,-22} {Outcome,-16} req {Requirements,2} · pts {PointsFactors,2} · occ {Occupations,2} · " +
                    "unverified {Unverified,3} · publish {Publish}",
                    result.Slug, result.Outcome, result.Requirements, result.PointsFactors, result.Occupations,
                    result.UnverifiedValues, result.PublishBlockers == 0 ? "ready" : $"blocked ({result.PublishBlockers})");
            }
        }

        await LinkChainsAsync(links, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var mappings = mappingsFile is null
            ? (0, 0, 0, 0)
            : await UpsertLanguageMappingsAsync(mappingsFile, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var report = new SeedReport(results, mappings.Item1, mappings.Item2, mappings.Item3, mappings.Item4);
        LogSummary(report);
        return report;
    }

    private async Task<Country> UpsertCountryAsync(Country seed, CancellationToken ct)
    {
        var country = await db.Countries.SingleOrDefaultAsync(c => c.Code == seed.Code, ct);
        if (country is null)
        {
            db.Countries.Add(seed);
            return seed;
        }

        country.Name = seed.Name;
        country.OccupationSystem = seed.OccupationSystem;
        country.LanguageScale = seed.LanguageScale;
        return country;
    }

    private async Task<(Pathway, SeedOutcome)> UpsertPathwayAsync(Country country, Pathway seed, CancellationToken ct)
    {
        var existing = await db.Pathways
            .Include(p => p.Requirements)
            .Include(p => p.PointsFactors)
            .Include(p => p.Occupations)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Slug == seed.Slug, ct);

        if (existing is null)
        {
            seed.Country = country;
            db.Pathways.Add(seed);
            return (seed, SeedOutcome.Created);
        }

        if (existing.Status != RuleStatus.Draft)
        {
            logger.LogWarning("  {Slug} is {Status}; leaving it unchanged", existing.Slug, existing.Status);
            return (existing, SeedOutcome.SkippedPublished);
        }

        existing.Country = country;
        existing.Name = seed.Name;
        existing.Type = seed.Type;
        existing.Audience = seed.Audience;
        existing.IsPointsBased = seed.IsPointsBased;
        existing.PassMark = seed.PassMark;
        existing.Cutoff = seed.Cutoff;
        existing.CutoffAsOf = seed.CutoffAsOf;
        existing.TypicalDurationMonths = seed.TypicalDurationMonths;
        existing.Summary = seed.Summary;
        existing.SourceUrl = seed.SourceUrl;
        existing.VerifiedAt = seed.VerifiedAt;
        existing.UnverifiedFields = seed.UnverifiedFields;

        db.Requirements.RemoveRange(existing.Requirements);
        db.PointsFactors.RemoveRange(existing.PointsFactors);
        db.OccupationLists.RemoveRange(existing.Occupations);
        existing.Requirements = seed.Requirements;
        existing.PointsFactors = seed.PointsFactors;
        existing.Occupations = seed.Occupations;

        return (existing, SeedOutcome.Updated);
    }

    private async Task LinkChainsAsync(List<(Pathway Pathway, string? NextSlug)> links, CancellationToken ct)
    {
        var bySlug = links.ToDictionary(l => l.Pathway.Slug, l => l.Pathway);
        foreach (var (pathway, nextSlug) in links)
        {
            if (nextSlug is null)
            {
                pathway.NextPathway = null;
                pathway.NextPathwayId = null;
                continue;
            }

            pathway.NextPathway = bySlug.GetValueOrDefault(nextSlug)
                ?? await db.Pathways.SingleOrDefaultAsync(p => p.Slug == nextSlug, ct)
                ?? throw new SeedException($"{pathway.Slug}: nextPathwaySlug '{nextSlug}' does not exist.");
        }
    }

    private async Task<(int Created, int Updated, int Skipped, int Unverified)> UpsertLanguageMappingsAsync(
        string file, CancellationToken ct)
    {
        var seeds = SeedParser.ParseLanguageMappings(await File.ReadAllTextAsync(file, ct), Path.GetFileName(file));
        var existing = await db.LanguageScoreMappings.ToListAsync(ct);
        var byKey = existing.ToDictionary(Key);
        int created = 0, updated = 0, skipped = 0;

        foreach (var seed in seeds)
        {
            if (!byKey.TryGetValue(Key(seed), out var mapping))
            {
                db.LanguageScoreMappings.Add(seed);
                created++;
            }
            else if (mapping.Status != RuleStatus.Draft)
            {
                skipped++;
            }
            else
            {
                mapping.MinScore = seed.MinScore;
                mapping.SourceUrl = seed.SourceUrl;
                mapping.VerifiedAt = seed.VerifiedAt;
                mapping.UnverifiedFields = seed.UnverifiedFields;
                updated++;
            }
        }

        var seedKeys = seeds.Select(Key).ToHashSet();
        db.LanguageScoreMappings.RemoveRange(
            existing.Where(m => m.Status == RuleStatus.Draft && !seedKeys.Contains(Key(m))));

        var unverified = seeds.Count(m => PublishGuard.Check(m).Count > 0);
        logger.LogInformation(
            "{File} → {Total} mappings: {Created} created, {Updated} updated, {Skipped} published (unchanged) · " +
            "publish blocked for {Unverified}",
            Path.GetFileName(file), seeds.Count, created, updated, skipped, unverified);
        return (created, updated, skipped, unverified);

        static (string, string, string, string) Key(LanguageScoreMapping m) =>
            (m.Test, m.Skill, m.TargetScale, m.TargetLevel);
    }

    private static PathwaySeedResult Summarise(Pathway pathway, SeedOutcome outcome) => new(
        pathway.Slug,
        outcome,
        pathway.Requirements.Count,
        pathway.PointsFactors.Count,
        pathway.Occupations.Count,
        pathway.UnverifiedFields.Count
            + pathway.Requirements.Sum(r => r.UnverifiedFields.Count)
            + pathway.PointsFactors.Sum(f => f.UnverifiedFields.Count)
            + pathway.Occupations.Sum(o => o.UnverifiedFields.Count),
        PublishGuard.Check(pathway).Count);

    private void LogSummary(SeedReport report)
    {
        var p = report.Pathways;
        logger.LogInformation(
            "Seed complete: {Total} pathways ({Created} created, {Updated} updated, {Skipped} published unchanged) · " +
            "{Unverified} unverified values · {Ready}/{Total} pathways publishable",
            p.Count,
            p.Count(r => r.Outcome == SeedOutcome.Created),
            p.Count(r => r.Outcome == SeedOutcome.Updated),
            p.Count(r => r.Outcome == SeedOutcome.SkippedPublished),
            p.Sum(r => r.UnverifiedValues),
            p.Count(r => r.PublishBlockers == 0),
            p.Count);
    }
}
