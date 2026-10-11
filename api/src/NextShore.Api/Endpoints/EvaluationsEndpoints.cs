using NextShore.Api.Setup;
using NextShore.Application.Evaluations;
using NextShore.Application.Profiles;
using NextShore.Application.Results;

namespace NextShore.Api.Endpoints;

public static class EvaluationsEndpoints
{
    public static IEndpointRouteBuilder MapEvaluationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/evaluations", async (CandidateProfileRequest profile, EvaluateHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(profile, ct);
                return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();
            })
            .WithName("Evaluate")
            .WithSummary("What-if evaluation of a profile. Not stored, no AI.")
            .Produces<EvaluationResponse>()
            .ProducesValidationProblem()
            .RequireRateLimiting(RateLimitPolicies.Evaluations);

        return app;
    }
}
