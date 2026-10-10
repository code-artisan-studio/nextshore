using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NextShore.Api.Setup;

namespace NextShore.Api.IntegrationTests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task Evaluations_policy_rejects_the_61st_request_with_problem_details()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddNextShoreRateLimiting();

        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapGet("/limited", () => Results.Ok()).RequireRateLimiting(RateLimitPolicies.Evaluations);
        await app.StartAsync();

        var client = app.GetTestClient();
        for (var i = 0; i < 60; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/limited")).StatusCode);
        }

        var rejected = await client.GetAsync("/limited");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }
}
