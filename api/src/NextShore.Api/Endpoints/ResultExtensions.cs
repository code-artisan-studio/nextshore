using NextShore.Domain;

namespace NextShore.Api.Endpoints;

/// <summary>Maps expected failures to RFC 7807 responses.</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Kind switch
    {
        ErrorKind.Validation => TypedResults.ValidationProblem(
            error.Details!.ToDictionary(kv => kv.Key, kv => kv.Value), title: error.Message),
        ErrorKind.NotFound => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found", detail: error.Message),
        ErrorKind.Unprocessable => TypedResults.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity, title: "Could not process", detail: error.Message),
        ErrorKind.Upstream => TypedResults.Problem(
            statusCode: StatusCodes.Status502BadGateway, title: "Upstream service failed", detail: error.Message),
        _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };
}
