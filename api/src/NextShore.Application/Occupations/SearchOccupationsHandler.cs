using NextShore.Application.Abstractions;

namespace NextShore.Application.Occupations;

public sealed record OccupationResponse(string System, string Code, string Title);

/// <summary>
/// <c>GET /occupations?q=&amp;system=</c>: type-ahead over published occupation list entries (any eligibility),
/// one per system + code. Code-prefix matches rank before title matches; at most <see cref="MaxResults"/>.
/// </summary>
public sealed class SearchOccupationsHandler(IRuleRepository rules)
{
    public const int MaxResults = 20;

    public async Task<IReadOnlyList<OccupationResponse>> HandleAsync(
        string? query, string? system, CancellationToken cancellationToken)
    {
        var snapshot = await rules.GetSnapshotAsync(cancellationToken);
        var systemByCountry = snapshot.Countries.ToDictionary(c => c.Code, c => c.OccupationSystem);
        var q = query?.Trim() ?? string.Empty;

        return snapshot.Pathways
            .SelectMany(p => p.Occupations.Select(o =>
                new OccupationResponse(systemByCountry.GetValueOrDefault(p.CountryCode, "?"), o.Code, o.Title)))
            .Where(o => system is null || string.Equals(o.System, system, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(o => (o.System, o.Code))
            .Select(o => (Occupation: o, Rank: Rank(o, q)))
            .Where(x => x.Rank is not null)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Occupation.Code, StringComparer.Ordinal)
            .Take(MaxResults)
            .Select(x => x.Occupation)
            .ToList();
    }

    private static int? Rank(OccupationResponse occupation, string query) =>
        query.Length == 0 || occupation.Code.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0
        : occupation.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1
        : null;
}
