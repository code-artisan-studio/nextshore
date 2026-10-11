using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NextShore.Api.Endpoints;
using NextShore.Infrastructure;

namespace NextShore.Api.IntegrationTests;

public sealed class PlatformTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AllowedOrigin = "https://web.nextshore.test";

    // Nothing listens on port 1, so the database is unreachable. Real-database tests come with T24.
    private const string UnreachableDb = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2";

    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(builder => builder
        .UseSetting("Cors:Origins", AllowedOrigin)
        .UseSetting("ConnectionStrings:Db", UnreachableDb));

    [Fact]
    public async Task Health_returns_ok_when_checks_pass()
    {
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Configure<HealthCheckServiceOptions>(options =>
                    options.Registrations.Remove(
                        options.Registrations.Single(r => r.Name == DependencyInjection.DatabaseHealthCheck)))))
            .CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", body?.Status);
    }

    [Fact]
    public async Task Health_returns_503_when_database_is_unreachable()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("unhealthy", body?.Status);
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem?.Status);
    }

    [Theory]
    [InlineData(AllowedOrigin, true)]
    [InlineData("https://evil.example", false)]
    public async Task Cors_allows_only_configured_origins(string origin, bool allowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
