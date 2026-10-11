using NextShore.Application.Assessments;
using NextShore.Application.Profiles;

namespace NextShore.Api.Endpoints;

public static class AssessmentsEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/assessments", async (CandidateProfileRequest profile, CreateAssessmentHandler handler, HttpContext http, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(profile, ct);
                return result.IsSuccess
                    ? TypedResults.Created($"{http.Request.PathBase}/api/v1/assessments/{result.Value.Id}", result.Value)
                    : result.Error!.ToProblem();
            })
            .WithName("CreateAssessment")
            .WithSummary("Evaluate a profile and store it (anonymous, expires after 30 days)")
            .Produces<AssessmentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        app.MapGet("/assessments/{id:guid}", async (Guid id, GetAssessmentHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();
            })
            .WithName("GetAssessment")
            .WithSummary("A stored assessment, exactly as it was evaluated")
            .Produces<AssessmentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
