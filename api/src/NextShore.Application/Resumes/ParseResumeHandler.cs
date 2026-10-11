using Microsoft.Extensions.Logging;
using NextShore.Application.Abstractions;
using NextShore.Domain;

namespace NextShore.Application.Resumes;

/// <summary>
/// <c>POST /resumes/parse</c>: checks consent, size and type, extracts a profile with the AI, then keeps only
/// occupation codes that exist in the published rules. The file is held in memory and never stored or logged.
/// </summary>
public sealed class ParseResumeHandler(IResumeParser parser, IRuleRepository rules, ILogger<ParseResumeHandler> logger)
{
    public async Task<Result<ExtractedProfile>> HandleAsync(byte[]? content, bool? consent, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (consent != true)
        {
            errors["consent"] = ["Agree to the resume being processed to continue."];
        }

        ResumeFormat? format = null;
        if (content is null || content.Length == 0)
        {
            errors["file"] = ["Upload a PDF or DOCX resume."];
        }
        else if (content.Length > ResumeFile.MaxBytes)
        {
            errors["file"] = ["The file is larger than 5 MB."];
        }
        else if ((format = ResumeFile.Detect(content)) is null)
        {
            errors["file"] = ["Only PDF or DOCX files are supported."];
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors.AsReadOnly());
        }

        ExtractedProfile profile;
        try
        {
            profile = await parser.ParseAsync(new ResumeDocument(format!.Value, content!), cancellationToken);
        }
        catch (ResumeUnreadableException ex)
        {
            // Format and reason only — never the file name or any content.
            logger.LogInformation("Resume ({Format}) could not be read: {Reason}", format, ex.Message);
            return Error.Unprocessable("We couldn't read this resume. You can fill in your profile manually instead.");
        }
        catch (AiProviderException ex)
        {
            logger.LogWarning("Resume ({Format}) parse failed: {Failure} {Reason}", format, ex.Failure, ex.Message);
            return Error.Upstream("Resume analysis is unavailable right now. Try again shortly or fill in your profile manually.");
        }

        return await CleanAsync(profile, cancellationToken);
    }

    /// <summary>Clamps confidences to 0–1 and drops occupation suggestions that are not published codes.</summary>
    private async Task<ExtractedProfile> CleanAsync(ExtractedProfile profile, CancellationToken cancellationToken)
    {
        var snapshot = await rules.GetSnapshotAsync(cancellationToken);
        var systemByCountry = snapshot.Countries.ToDictionary(c => c.Code, c => c.OccupationSystem);
        var knownCodes = snapshot.Pathways
            .SelectMany(p => p.Occupations.Select(o => (System: systemByCountry.GetValueOrDefault(p.CountryCode), o.Code)))
            .ToHashSet();

        static decimal Clamp(decimal confidence) => Math.Clamp(confidence, 0m, 1m);

        return profile with
        {
            Education = profile.Education.Select(e => e with { Confidence = Clamp(e.Confidence) }).ToList(),
            Work = profile.Work.Select(w => w with { Confidence = Clamp(w.Confidence) }).ToList(),
            TotalSkilledYears = profile.TotalSkilledYears with
            {
                Value = profile.TotalSkilledYears.Value is { } years ? Math.Max(years, 0) : null,
                Confidence = Clamp(profile.TotalSkilledYears.Confidence),
            },
            LanguagesMentioned = profile.LanguagesMentioned.Select(l => l with { Confidence = Clamp(l.Confidence) }).ToList(),
            OccupationSuggestions = profile.OccupationSuggestions
                .Where(o => knownCodes.Contains((o.System.ToUpperInvariant(), o.Code.Trim())))
                .Select(o => o with { System = o.System.ToUpperInvariant(), Code = o.Code.Trim(), Confidence = Clamp(o.Confidence) })
                .DistinctBy(o => (o.System, o.Code))
                .ToList(),
        };
    }
}
