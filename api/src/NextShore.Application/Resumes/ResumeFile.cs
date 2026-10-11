using System.IO.Compression;
using NextShore.Application.Abstractions;

namespace NextShore.Application.Resumes;

/// <summary>Identifies a resume by its content (magic bytes), never by file name or declared content type.</summary>
public static class ResumeFile
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04];

    public static ResumeFormat? Detect(byte[] content)
    {
        if (content.AsSpan().StartsWith(PdfMagic))
        {
            return ResumeFormat.Pdf;
        }

        if (!content.AsSpan().StartsWith(ZipMagic))
        {
            return null;
        }

        try
        {
            using var zip = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
            return zip.GetEntry("word/document.xml") is not null ? ResumeFormat.Docx : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
