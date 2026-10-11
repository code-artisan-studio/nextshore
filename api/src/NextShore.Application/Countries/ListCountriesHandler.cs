using NextShore.Application.Abstractions;
using NextShore.Application.Common;

namespace NextShore.Application.Countries;

public sealed record CountryResponse(string Code, string Name, IReadOnlyList<PathwaySummaryResponse> Pathways);

public sealed record PathwaySummaryResponse(string Slug, string Name, string Type, bool IsPointsBased, string Summary);

/// <summary><c>GET /countries</c>: every country with its published pathways.</summary>
public sealed class ListCountriesHandler(IRuleRepository rules)
{
    public async Task<IReadOnlyList<CountryResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var snapshot = await rules.GetSnapshotAsync(cancellationToken);
        return snapshot.Countries
            .Select(country => new CountryResponse(
                country.Code,
                country.Name,
                snapshot.Pathways
                    .Where(p => p.CountryCode == country.Code)
                    .Select(p => new PathwaySummaryResponse(p.Slug, p.Name, EnumText.Of(p.Type), p.IsPointsBased, p.Summary))
                    .ToList()))
            .ToList();
    }
}
