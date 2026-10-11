using FluentValidation;
using FluentValidation.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextShore.Application.Assessments;
using NextShore.Application.Countries;
using NextShore.Application.Engine;
using NextShore.Application.Evaluations;
using NextShore.Application.Occupations;
using NextShore.Application.Profiles;

namespace NextShore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Validation error keys are camelCase JSON paths, e.g. "english.scores.listening".
        ValidatorOptions.Global.PropertyNameResolver = (_, member, expression) =>
        {
            var path = expression is null ? member?.Name : PropertyChain.FromExpression(expression).ToString();
            return string.IsNullOrEmpty(path) ? member?.Name
                : string.Join('.', path.Split('.').Select(part => char.ToLowerInvariant(part[0]) + part[1..]));
        };

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IValidator<CandidateProfileRequest>, CandidateProfileValidator>();
        services.AddScoped<EngineRunner>();
        services.AddScoped<ListCountriesHandler>();
        services.AddScoped<SearchOccupationsHandler>();
        services.AddScoped<EvaluateHandler>();
        services.AddScoped<CreateAssessmentHandler>();
        services.AddScoped<GetAssessmentHandler>();
        return services;
    }
}
