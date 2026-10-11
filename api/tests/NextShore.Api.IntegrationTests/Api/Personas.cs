using System.Text.Json;
using NextShore.Application.Profiles;

namespace NextShore.Api.IntegrationTests.Api;

/// <summary>The fictional demo personas as API requests (same data as the engine golden tests).</summary>
internal static class Personas
{
    private static readonly Dictionary<string, decimal> NoYearsAbroad = new() { ["CA"] = 0, ["DE"] = 0, ["AU"] = 0 };

    private static JobOfferRequest JobOffer(bool de, decimal? salaryEur = null) => new()
    {
        SalaryEur = salaryEur,
        Countries = new Dictionary<string, JsonElement>
        {
            ["CA"] = JsonSerializer.SerializeToElement(false),
            ["DE"] = JsonSerializer.SerializeToElement(de),
            ["AU"] = JsonSerializer.SerializeToElement(false),
        },
    };

    public static CandidateProfileRequest Student => new()
    {
        Goal = "study",
        DateOfBirth = new DateOnly(2004, 5, 1),
        Citizenship = "IN",
        Education = new EducationRequest("bachelors", "Computer Science", 4),
        Work = new WorkRequest(0.5m, NoYearsAbroad),
        English = new EnglishRequest("ielts", "general", new ScoresRequest(7.5m, 7.0m, 6.5m, 7.0m)),
        FundsUsd = 30000,
        MaritalStatus = "single",
        JobOffer = JobOffer(de: false),
        RelativeIn = new() { ["CA"] = false, ["AU"] = false },
    };

    public static CandidateProfileRequest MidDeveloper => new()
    {
        Goal = "pr",
        DateOfBirth = new DateOnly(1997, 3, 15),
        Citizenship = "IN",
        Education = new EducationRequest("bachelors", "Computer Science", 4),
        Work = new WorkRequest(5, NoYearsAbroad),
        OccupationCodes = new() { ["NOC"] = "21231", ["ISCO"] = "2512", ["ANZSCO"] = "261313" },
        English = new EnglishRequest("ielts", "general", new ScoresRequest(8.0m, 7.0m, 7.0m, 7.5m)),
        German = new GermanRequest("A2"),
        FundsUsd = 25000,
        MaritalStatus = "single",
        JobOffer = JobOffer(de: true, salaryEur: 52000),
        RelativeIn = new() { ["CA"] = false, ["AU"] = false },
        DegreeRecognizedDE = true,
    };
}
