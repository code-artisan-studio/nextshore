using Microsoft.AspNetCore.Mvc;
using NextShore.Api.Setup;
using NextShore.Application.Resumes;

namespace NextShore.Api.Endpoints;

public static class ResumesEndpoints
{
    /// <summary>Room for the 5 MB file plus multipart overhead; anything bigger is refused before it is read.</summary>
    private const long MaxRequestBytes = ResumeFile.MaxBytes + 512 * 1024;

    public static IEndpointRouteBuilder MapResumesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/resumes/parse", async (IFormFile? file, [FromForm] bool? consent, ParseResumeHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(await ReadAsync(file, ct), consent, ct);
                return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();
            })
            .WithName("ParseResume")
            .WithSummary("Extract a profile from a PDF or DOCX resume (≤ 5 MB). The file is never stored.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ExtractedProfile>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .DisableAntiforgery() // no cookies or sessions: nothing for a cross-site form to ride on
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .RequireRateLimiting(RateLimitPolicies.ResumeParse);

        return app;
    }

    /// <summary>Reads at most one byte past the limit, so the handler can report "too large" without buffering more.</summary>
    private static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        await using var stream = file.OpenReadStream();
        var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length <= ResumeFile.MaxBytes && (read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
