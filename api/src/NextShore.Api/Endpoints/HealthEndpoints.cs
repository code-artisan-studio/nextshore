using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NextShore.Api.Endpoints;

public sealed record HealthResponse(string Status);

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteResponse });
        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(
            report.Status == HealthStatus.Healthy ? "ok" : report.Status.ToString().ToLowerInvariant()));
}
