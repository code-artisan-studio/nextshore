using System.Text.Json;
using Anthropic.Models.Messages;
using NextShore.Application.Abstractions;
using NextShore.Application.Resumes;
using NextShore.Infrastructure.Documents;

namespace NextShore.Infrastructure.Ai;

/// <summary>
/// Resume → <see cref="ExtractedProfile"/> with Claude structured output. PDFs go as a document block, DOCX as
/// extracted text. Nothing is stored or logged.
/// </summary>
public sealed class ClaudeResumeParser(ClaudeClient claude, TimeProvider clock) : IResumeParser
{
    /// <summary>Thinking counts toward this, so it leaves room beyond the JSON itself.</summary>
    public const int MaxTokens = 8000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ExtractedProfile> ParseAsync(ResumeDocument document, CancellationToken cancellationToken)
    {
        // DOCX is read before calling Claude so an unreadable file never reaches the provider.
        List<ContentBlockParam> content = document.Format switch
        {
            ResumeFormat.Pdf =>
            [
                new DocumentBlockParam { Source = new Base64PdfSource { Data = Convert.ToBase64String(document.Content) } },
                new TextBlockParam { Text = Instruction() },
            ],
            _ =>
            [
                new TextBlockParam { Text = $"<resume>\n{DocxText.Extract(document.Content)}\n</resume>" },
                new TextBlockParam { Text = Instruction() },
            ],
        };

        var message = await claude.SendAsync(ClaudePurpose.ResumeParse, (client, model, ct) =>
            client.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = MaxTokens,
                System = ResumeExtractionPrompt.System,
                OutputConfig = new OutputConfig
                {
                    Effort = Effort.Low,
                    Format = new JsonOutputFormat { Schema = ResumeExtractionPrompt.Schema },
                },
                Messages = [new() { Role = Role.User, Content = content }],
            }, ct), cancellationToken);

        if (message.StopReason == "refusal")
        {
            throw new ResumeUnreadableException("The model declined to read this document.");
        }

        if (message.StopReason == "max_tokens")
        {
            throw new AiProviderException(AiFailure.Unavailable, "Resume extraction ran out of output tokens.");
        }

        var json = message.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text).FirstOrDefault();
        try
        {
            return JsonSerializer.Deserialize<ExtractedProfile>(json ?? string.Empty, Json)
                ?? throw new JsonException("Empty extraction.");
        }
        catch (JsonException ex)
        {
            throw new AiProviderException(AiFailure.Unavailable, "Resume extraction returned invalid JSON.", ex);
        }
    }

    private string Instruction() =>
        $"Today is {DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime):yyyy-MM-dd}. Extract the profile from this resume.";
}
