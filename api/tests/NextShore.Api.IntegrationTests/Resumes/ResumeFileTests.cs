using System.IO.Compression;
using NextShore.Application.Abstractions;
using NextShore.Application.Resumes;
using NextShore.Infrastructure.Documents;

namespace NextShore.Api.IntegrationTests.Resumes;

public class ResumeFileTests
{
    [Fact]
    public void Pdf_and_docx_are_recognised_by_content()
    {
        Assert.Equal(ResumeFormat.Pdf, ResumeFile.Detect(TestResumes.Pdf));
        Assert.Equal(ResumeFormat.Docx, ResumeFile.Detect(TestResumes.Docx("Jane Doe")));
    }

    [Fact]
    public void Other_files_are_not_resumes()
    {
        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("xl/workbook.xml"); // a spreadsheet, not a Word document
        }

        Assert.Null(ResumeFile.Detect(zip.ToArray()));
        Assert.Null(ResumeFile.Detect("Plain text resume"u8.ToArray()));
        Assert.Null(ResumeFile.Detect([0x50, 0x4B, 0x03, 0x04, 0x00, 0x01])); // zip header, broken body
        Assert.Null(ResumeFile.Detect([0xFF, 0xD8, 0xFF, 0xE0]));             // JPEG
    }

    [Fact]
    public void Docx_text_is_one_line_per_paragraph()
    {
        var text = DocxText.Extract(TestResumes.Docx("Software Engineer, Acme", "", "2019-07 to present"));

        Assert.Equal("Software Engineer, Acme\n2019-07 to present", text);
    }

    [Fact]
    public void Empty_or_broken_docx_is_unreadable()
    {
        Assert.Throws<ResumeUnreadableException>(() => DocxText.Extract(TestResumes.Docx()));
        Assert.Throws<ResumeUnreadableException>(() => DocxText.Extract([0x50, 0x4B, 0x03, 0x04, 0x00]));
    }
}
