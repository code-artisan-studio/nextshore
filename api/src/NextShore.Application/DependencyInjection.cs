using Microsoft.Extensions.DependencyInjection;
using NextShore.Application.Countries;
using NextShore.Application.Occupations;

namespace NextShore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListCountriesHandler>();
        services.AddScoped<SearchOccupationsHandler>();
        return services;
    }
}
