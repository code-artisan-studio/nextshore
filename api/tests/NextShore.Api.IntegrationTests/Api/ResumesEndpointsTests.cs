using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using NextShore.Api.IntegrationTests.Resumes;
using NextShore.Application.Abstractions;
using NextShore.Application.Resumes;

namespace NextShore.Api.IntegrationTests.Api;

public sealed class ResumesEndpointsTests
{
    private sealed class FakeParser(Func<ResumeDocument, ExtractedProfile> parse) : IResumeParser
    {
        public List<ResumeFormat> Calls { get; } = [];

        public Task<ExtractedProfile> ParseAsync(ResumeDocument document, CancellationToken cancellationToken)
        {
            Calls.Add(document.Format);
            return Task.FromResult(parse(document));
        }
    }

    private static ExtractedProfile Profile(params OccupationSuggestion[] occupations) => new(
        [new ExtractedEducation("bachelors", "Computer Science", "IN", 2019, 1.4m)],
        [new ExtractedWork("Software Engineer", "Acme", "2019-07", null, "IN", true, 0.9m)],
        new ExtractedYears(5.2m, -0.2m),
        ["C#"], [], [], occupations);

    private static MultipartFormDataContent Form(byte[]? file, string? consent = "true", string fileName = "resume.pdf")
    {
        var form = new MultipartFormDataContent();
        if (file is not null)
        {
            var content = new ByteArrayContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(content, "file", fileName);
        }

        if (consent is not null)
        {
            form.Add(new StringContent(consent), "consent");
        }

        return form;
    }

    private static async Task<(HttpResponseMessage Response, FakeParser Parser)> Post(
        MultipartFormDataContent form, Func<ResumeDocument, ExtractedProfile>? parse = null)
    {
        var parser = new FakeParser(parse ?? (_ => Profile()));
        using var factory = new ApiFactory { ResumeParser = parser };
        var response = await factory.CreateClient().PostAsync("/api/v1/resumes/parse", form);
        return (response, parser);
    }

    [Theory]
    [InlineData("resume.pdf", ResumeFormat.Pdf)]
    [InlineData("resume.docx", ResumeFormat.Docx)]
    public async Task Pdf_and_docx_are_parsed(string fileName, ResumeFormat format)
    {
        var bytes = format == ResumeFormat.Pdf ? TestResumes.Pdf : TestResumes.Docx("Software Engineer");

        var (response, parser) = await Post(Form(bytes, fileName: fileName));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([format], parser.Calls);
        var profile = (await response.Content.ReadFromJsonAsync<ExtractedProfile>())!;
        Assert.Equal("Software Engineer", profile.Work[0].Title);
    }

    [Fact]
    public async Task Type_is_decided_by_content_not_file_name()
    {
        var (response, parser) = await Post(Form("Plain text"u8.ToArray(), fileName: "resume.pdf"));

        await AssertValidation(response, "file", "Only PDF or DOCX files are supported.");
        Assert.Empty(parser.Calls);
    }

    [Theory]
    [InlineData(null, "consent")]
    [InlineData("false", "consent")]
    public async Task Consent_is_required(string? consent, string field)
    {
        var (response, parser) = await Post(Form(TestResumes.Pdf, consent));

        await AssertValidation(response, field, "Agree to the resume being processed to continue.");
        Assert.Empty(parser.Calls);
    }

    [Fact]
    public async Task A_file_is_required()
    {
        var (response, _) = await Post(Form(null));

        await AssertValidation(response, "file", "Upload a PDF or DOCX resume.");
    }

    [Fact]
    public async Task Files_over_5_mb_are_refused()
    {
        var big = new byte[ResumeFile.MaxBytes + 1];
        TestResumes.Pdf.CopyTo(big, 0);

        var (response, parser) = await Post(Form(big));

        await AssertValidation(response, "file", "The file is larger than 5 MB.");
        Assert.Empty(parser.Calls);
    }

    [Fact]
    public async Task Only_published_occupation_codes_are_kept_and_confidence_is_clamped()
    {
        var (response, _) = await Post(Form(TestResumes.Pdf), _ => Profile(
            new OccupationSuggestion("noc", "21231", "Software engineers and designers", 0.8m),
            new OccupationSuggestion("NOC", "99999", "Invented", 0.9m),
            new OccupationSuggestion("ANZSCO", "261313", "Software Engineer", 0.8m))); // ANZSCO entries are unpublished

        var profile = (await response.Content.ReadFromJsonAsync<ExtractedProfile>())!;

        Assert.Equal([("NOC", "21231")], profile.OccupationSuggestions.Select(o => (o.System, o.Code)));
        Assert.Equal(1m, profile.Education[0].Confidence);
        Assert.Equal(0m, profile.TotalSkilledYears.Confidence);
    }

    [Fact]
    public async Task Unreadable_resume_is_422()
    {
        var (response, _) = await Post(Form(TestResumes.Pdf), _ => throw new ResumeUnreadableException("test"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("fill in your profile manually", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    }

    [Fact]
    public async Task Ai_failure_is_502()
    {
        var (response, _) = await Post(Form(TestResumes.Pdf), _ => throw new AiProviderException(AiFailure.Timeout, "test"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Parsing_is_limited_to_10_per_day()
    {
        using var factory = new ApiFactory { ResumeParser = new FakeParser(_ => Profile()) };
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/resumes/parse", Form(TestResumes.Pdf))).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/api/v1/resumes/parse", Form(TestResumes.Pdf))).StatusCode);
    }

    private static async Task AssertValidation(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal([message], problem.Errors[field]);
    }
}
