using System.Net;
using System.Net.Http.Json;
using NextShore.Api.Endpoints;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class AdminAuthTests(AdminFactory factory) : IClassFixture<AdminFactory>
{
    [Fact]
    public async Task Admin_token_reaches_admin_endpoints()
    {
        var me = await factory.CreateAdminClient().GetFromJsonAsync<AdminMeResponse>("/api/v1/admin/me");

        Assert.Equal(new AdminMeResponse("user_admin", "admin"), me);
    }

    [Fact]
    public async Task Missing_token_is_401_problem()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("member")]
    [InlineData("Admin")] // claim values are compared exactly
    public async Task Signed_in_non_admin_is_403(string? role)
    {
        var response = await factory.CreateAdminClient(AdminFactory.Token(role: role)).GetAsync("/api/v1/admin/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static TheoryData<string> InvalidTokens => new()
    {
        AdminFactory.Token(expires: DateTime.UtcNow.AddMinutes(-5)),
        AdminFactory.Token(issuer: "https://clerk.other.example"),
        AdminFactory.Token(azp: "https://evil.example"),
        AdminFactory.Token()[..^4] + "AAAA", // tampered signature
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Invalid_admin_token_is_401(string token)
    {
        var response = await factory.CreateAdminClient(token).GetAsync("/api/v1/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Public_endpoints_stay_anonymous()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Without_clerk_authority_every_admin_request_is_401()
    {
        using var unconfigured = new ApiFactory();

        var client = unconfigured.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", AdminFactory.Token());
        var response = await client.GetAsync("/api/v1/admin/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
