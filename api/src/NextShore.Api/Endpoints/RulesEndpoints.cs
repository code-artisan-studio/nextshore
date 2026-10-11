using NextShore.Application.Countries;
using NextShore.Application.Occupations;

namespace NextShore.Api.Endpoints;

public static class RulesEndpoints
{
    public static IEndpointRouteBuilder MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/countries", async (ListCountriesHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(ct)))
            .WithName("ListCountries")
            .WithSummary("Countries with their published pathways");

        app.MapGet("/occupations", async (string? q, string? system, SearchOccupationsHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.HandleAsync(q, system, ct)))
            .WithName("SearchOccupations")
            .WithSummary("Type-ahead over published occupation codes (max 20)");

        return app;
    }
}
