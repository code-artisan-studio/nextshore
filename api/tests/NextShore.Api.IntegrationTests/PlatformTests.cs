using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NextShore.Api.Endpoints;

namespace NextShore.Api.IntegrationTests;

public sealed class PlatformTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AllowedOrigin = "https://web.nextshore.test";

    private readonly WebApplicationFactory<Program> _factory =
        factory.WithWebHostBuilder(builder => builder.UseSetting("Cors:Origins", AllowedOrigin));

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", body?.Status);
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
