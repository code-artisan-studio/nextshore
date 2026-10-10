using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace NextShore.Api.Setup;

public static class RateLimitPolicies
{
    /// <summary>10 requests per IP per day.</summary>
    public const string ResumeParse = "resume-parse";

    /// <summary>60 requests per IP per minute.</summary>
    public const string Evaluations = "evaluations";
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddNextShoreRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.ResumeParse, context =>
                PerIp(context, permitLimit: 10, window: TimeSpan.FromDays(1)));
            options.AddPolicy(RateLimitPolicies.Evaluations, context =>
                PerIp(context, permitLimit: 60, window: TimeSpan.FromMinutes(1)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new()
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Rate limit exceeded. Please try again later.",
                    },
                });
            };
        });

    private static RateLimitPartition<string> PerIp(HttpContext context, int permitLimit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 });
}
