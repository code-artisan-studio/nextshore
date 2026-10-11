using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextShore.Application.Profiles;
using NextShore.Application.Results;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class EvaluationsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<HttpResponseMessage> Post(object body) =>
        await factory.CreateClient().PostAsJsonAsync("/api/v1/evaluations", body);

    [Fact]
    public async Task Developer_gets_ranked_results_and_no_chains()
    {
        var response = await Post(Personas.MidDeveloper);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<EvaluationResponse>())!;
        Assert.Equal(
            [("au-189", "eligible", 100), ("de-blue-card", "eligible", 100), ("ca-express-entry", "not_eligible", 88),
             ("de-opportunity-card", "not_eligible", 87)],
            body.Results.Select(r => (r.PathwaySlug, r.Grade, r.ReadinessScore)));
        Assert.Empty(body.Chains);

        var au189 = body.Results[0];
        Assert.Equal("Skilled Independent visa (subclass 189)", au189.PathwayName);
        Assert.Equal(("default", 75, 65, "pass_mark"), (au189.Points!.Variant, au189.Points.Total, au189.Points.Target, au189.Points.TargetKind));
        var expressEntry = body.Results[2];
        Assert.Equal(("single", 357, "cutoff"), (expressEntry.Points!.Variant, expressEntry.Points.Total, expressEntry.Points.TargetKind));
        Assert.NotEmpty(expressEntry.Gaps);
        Assert.All(expressEntry.Sources, s => Assert.StartsWith("https://", s.Url));
    }

    [Fact]
    public async Task Student_gets_chains_with_projected_steps()
    {
        var body = (await (await Post(Personas.Student)).Content.ReadFromJsonAsync<EvaluationResponse>())!;

        Assert.Equal(["CA", "DE"], body.Chains.Select(c => c.CountryCode));
        var canada = body.Chains[0];
        Assert.Equal(["study", "post_study_work", "pr"], canada.Steps.Select(s => s.Type));
        Assert.Equal([false, true, true], canada.Steps.Select(s => s.IsProjected));
        Assert.Null(canada.TotalMonths);
    }

    [Fact]
    public async Task Json_uses_camel_case_names_and_snake_case_values()
    {
        var json = await (await Post(Personas.MidDeveloper)).Content.ReadAsStringAsync();

        Assert.Contains("\"pathwaySlug\":\"ca-express-entry\"", json);
        Assert.Contains("\"grade\":\"not_eligible\"", json);
        Assert.Contains("\"targetKind\":\"pass_mark\"", json);
        Assert.Contains("\"fixability\":\"", json);
    }

    [Fact]
    public async Task Empty_profile_is_rejected_with_every_missing_field()
    {
        var response = await Post(new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(["citizenship", "dateOfBirth", "education", "goal", "maritalStatus", "work"], problem.Errors.Keys.Order());
    }

    public static TheoryData<string, Func<CandidateProfileRequest, CandidateProfileRequest>> InvalidProfiles => new()
    {
        { "goal", p => p with { Goal = "retire" } },
        { "dateOfBirth", p => p with { DateOfBirth = new DateOnly(2030, 1, 1) } },
        { "citizenship", p => p with { Citizenship = "india" } },
        { "education.level", p => p with { Education = p.Education! with { Level = "doctorate" } } },
        { "work.yearsSkilled", p => p with { Work = p.Work! with { YearsSkilled = -1 } } },
        { "english.testVariant", p => p with { English = p.English! with { TestVariant = null } } },
        { "english.scores.listening", p => p with { English = p.English! with { Scores = p.English.Scores! with { Listening = 9.5m } } } },
        { "english.scores.reading", p => p with { English = p.English! with { Scores = p.English.Scores! with { Reading = 6.3m } } } },
        { "german.level", p => p with { German = new GermanRequest("D1") } },
        { "spouse", p => p with { Spouse = new SpouseRequest("masters", 8) } },
        { "fundsUsd", p => p with { FundsUsd = -5 } },
        { "work.yearsInCountry.CA", p => p with { Work = p.Work! with { YearsInCountry = new() { ["CA"] = 99 } } } },
        { "work.yearsInCountry.canada", p => p with { Work = p.Work! with { YearsInCountry = new() { ["canada"] = 1 } } } },
        { "occupationCodes.SOC", p => p with { OccupationCodes = new() { ["SOC"] = "15-1252" } } },
        { "relativeIn.ca", p => p with { RelativeIn = new() { ["ca"] = true } } },
    };

    [Theory]
    [MemberData(nameof(InvalidProfiles))]
    public async Task Invalid_field_is_reported_by_its_json_path(string field, Func<CandidateProfileRequest, CandidateProfileRequest> change)
    {
        var response = await Post(change(Personas.MidDeveloper));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal([field], problem.Errors.Keys);
    }

    [Fact]
    public async Task Job_offer_country_must_be_yes_or_no()
    {
        var profile = Personas.MidDeveloper with
        {
            JobOffer = new JobOfferRequest { Countries = new() { ["DE"] = JsonSerializer.SerializeToElement("yes") } },
        };

        var response = await Post(profile);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        var error = Assert.Single(problem.Errors);
        Assert.Equal("jobOffer.DE", error.Key);
        Assert.Equal(["Must be true or false."], error.Value);
    }

    [Fact]
    public async Task Evaluations_are_rate_limited_to_60_per_minute()
    {
        using var isolated = new ApiFactory();
        var client = isolated.CreateClient();
        var request = new { };

        for (var i = 0; i < 60; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/evaluations", request)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/v1/evaluations", request)).StatusCode);
    }
}
