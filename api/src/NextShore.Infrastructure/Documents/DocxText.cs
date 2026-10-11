using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NextShore.Application.Abstractions;

namespace NextShore.Infrastructure.Documents;

/// <summary>Plain text of a DOCX body, one line per paragraph. In memory only.</summary>
public static class DocxText
{
    public static string Extract(byte[] content)
    {
        try
        {
            using var document = WordprocessingDocument.Open(new MemoryStream(content, writable: false), isEditable: false);
            var body = document.MainDocumentPart?.Document?.Body;
            var text = body is null
                ? string.Empty
                : string.Join('\n', body.Descendants<Paragraph>().Select(p => p.InnerText).Where(t => !string.IsNullOrWhiteSpace(t)));

            return string.IsNullOrWhiteSpace(text)
                ? throw new ResumeUnreadableException("The DOCX file contains no text.")
                : text;
        }
        catch (Exception ex) when (ex is not ResumeUnreadableException and not OutOfMemoryException)
        {
            throw new ResumeUnreadableException("The DOCX file could not be opened.", ex);
        }
    }
}
