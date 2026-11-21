using System.Text;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// File attachments (embedded / associated files). Parity peer of the
/// TypeScript <c>tests/document/attachments.test.ts</c>; both derive from the
/// shared conformance behaviour for <c>/EmbeddedFiles</c>, <c>/Filespec</c>,
/// and <c>/AF</c>.
/// </summary>
[Collection("Serial")]
public class AttachmentTests
{
    private static byte[] Xml() => Encoding.UTF8.GetBytes("<invoice><total>42.00</total></invoice>");

    private static (string Pdf, BonniePDF.Document Doc) Generate(System.Action<BonniePDF.Document> populate)
    {
        var doc = new BonniePDF.Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return (Encoding.Latin1.GetString(ms.ToArray()), doc);
    }

    [Fact]
    public void EmbedsAttachment_AsEmbeddedFileStreamWithFilespec()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Add(new TextBlock("hello"));
            doc.Attach("invoice.xml", Xml(), AttachmentRelationship.Source, "text/xml");
        });
        using (doc)
        {
            Assert.Contains("/Type /EmbeddedFile", pdf);
            Assert.Contains("/Type /Filespec", pdf);
            Assert.Contains("/AFRelationship /Source", pdf);
            Assert.Contains("/Subtype /text#2Fxml", pdf);
            Assert.Contains("(invoice.xml)", pdf);
        }
    }

    [Fact]
    public void ReferencesEmbeddedFiles_FromCatalogNamesTreeAndAfArray()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Add(new TextBlock("hello"));
            doc.Attach("data.json", Encoding.UTF8.GetBytes("{\"a\":1}"), AttachmentRelationship.Data, "application/json");
        });
        using (doc)
        {
            Assert.Contains("/EmbeddedFiles", pdf);
            Assert.Matches(@"/AF \[ \d+ 0 R \]", pdf);
            Assert.Contains("/Names [ (data.json)", pdf);
        }
    }

    [Fact]
    public void OrdersMultipleAttachments_ByFileName()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Add(new TextBlock("hello"));
            doc.Attach("zeta.txt", Encoding.UTF8.GetBytes("z"));
            doc.Attach("alpha.txt", Encoding.UTF8.GetBytes("a"));
        });
        using (doc)
        {
            string names = pdf.Substring(pdf.IndexOf("/Names [ (", System.StringComparison.Ordinal));
            Assert.True(names.IndexOf("(alpha.txt)", System.StringComparison.Ordinal)
                < names.IndexOf("(zeta.txt)", System.StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EmitsAttachments_UnderPdfA3b_ZugferdPath()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Settings.Conformance = PdfAConformance.PdfA3b;
            doc.Add(new TextBlock("invoice"));
            doc.Attach("factur-x.xml", Xml(), AttachmentRelationship.Data, "text/xml");
        });
        using (doc)
        {
            Assert.Contains("/Type /EmbeddedFile", pdf);
            Assert.Contains("/AF [", pdf);
            Assert.DoesNotContain(doc.Diagnostics, d => d.Category == DiagnosticCategory.Attachment);
        }
    }

    [Fact]
    public void DropsAttachments_UnderPdfA1b_AndRecordsDiagnostic()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Settings.Conformance = PdfAConformance.PdfA1b;
            doc.Add(new TextBlock("archival"));
            doc.Attach("invoice.xml", Xml(), mediaType: "text/xml");
        });
        using (doc)
        {
            Assert.DoesNotContain("/Type /EmbeddedFile", pdf);
            Assert.DoesNotContain("/EmbeddedFiles", pdf);
            var warnings = doc.Diagnostics.Where(d => d.Category == DiagnosticCategory.Attachment).ToList();
            Assert.Single(warnings);
            Assert.Equal(DiagnosticSeverity.Warning, warnings[0].Severity);
        }
    }

    [Theory]
    [InlineData(PdfAConformance.None, true)]
    [InlineData(PdfAConformance.PdfA3b, true)]
    [InlineData(PdfAConformance.PdfA3a, true)]
    [InlineData(PdfAConformance.PdfA4, false)]
    [InlineData(PdfAConformance.PdfA4f, true)]
    [InlineData(PdfAConformance.PdfA1b, false)]
    [InlineData(PdfAConformance.PdfA1a, false)]
    [InlineData(PdfAConformance.PdfA2b, false)]
    [InlineData(PdfAConformance.PdfA2a, false)]
    public void AttachmentRules_MatchesSpec(PdfAConformance conformance, bool allowed)
    {
        Assert.Equal(allowed, AttachmentRules.AllowsEmbeddedFiles(conformance));
    }

    // Mirrors the shared fixture honest-claim/attachments (04 §10.1-10.3).
    [Theory]
    [InlineData("none", PdfAConformance.None, AccessibilityMode.None, "text/xml", true, null, null, "/text#2Fxml", 0)]
    [InlineData("pdfa1b", PdfAConformance.PdfA1b, AccessibilityMode.None, "text/xml", false, "1", "B", null, 1)]
    [InlineData("pdfa2b", PdfAConformance.PdfA2b, AccessibilityMode.None, "text/xml", false, "2", "B", null, 1)]
    [InlineData("pdfa3b", PdfAConformance.PdfA3b, AccessibilityMode.None, "text/xml", true, "3", "B", "/text#2Fxml", 0)]
    [InlineData("pdfa4", PdfAConformance.PdfA4, AccessibilityMode.None, "text/xml", false, "4", null, null, 1)]
    [InlineData("pdfa4-with-ua2", PdfAConformance.PdfA4, AccessibilityMode.PdfUa2, "text/xml", false, "4", null, null, 1)]
    [InlineData("pdfa4f", PdfAConformance.PdfA4f, AccessibilityMode.None, "text/xml", true, "4", "F", "/text#2Fxml", 0)]
    [InlineData("pdfa4f-with-ua2", PdfAConformance.PdfA4f, AccessibilityMode.PdfUa2, "text/xml", true, "4", "F", "/text#2Fxml", 1, "data.xml", null,
        "Attachment 'data.xml' has no description; PDF/UA-2 requires one, so its file name was used. Give it a description that tells readers what the file contains.")]
    [InlineData("pdfa4f-with-ua2-described", PdfAConformance.PdfA4f, AccessibilityMode.PdfUa2, "text/xml", true, "4", "F", "/text#2Fxml", 0, "Meter readings", "Meter readings")]
    [InlineData("none-no-media-type", PdfAConformance.None, AccessibilityMode.None, null, true, null, null, null, 0)]
    [InlineData("pdfa3b-no-media-type", PdfAConformance.PdfA3b, AccessibilityMode.None, null, true, "3", "B", "/application#2Foctet-stream", 0)]
    [InlineData("pdfa4f-no-media-type", PdfAConformance.PdfA4f, AccessibilityMode.None, null, true, "4", "F", "/application#2Foctet-stream", 0)]
    public void ConformanceGuard_MatchesFixture(
        string name, PdfAConformance conformance, AccessibilityMode accessibility, string? mediaType,
        bool embedded, string? part, string? level, string? subtype, int warnings, string? desc = null,
        string? description = null, string? warning = null)
    {
        Assert.NotEmpty(name);
        var (pdf, doc) = Generate(doc =>
        {
            doc.Settings.Conformance = conformance;
            doc.Settings.Accessibility = accessibility;
            doc.Add(new TextBlock("Attachment guard"));
            doc.Attach(new FileAttachment
            {
                FileName = "data.xml",
                Content = Encoding.UTF8.GetBytes("<a/>"),
                Relationship = AttachmentRelationship.Data,
                MediaType = mediaType,
                Description = description,
            });
        });
        using (doc)
        {
            foreach (string marker in new[] { "/Type /EmbeddedFile", "/EmbeddedFiles", "/AF " })
                Assert.Equal(embedded, pdf.Contains(marker, StringComparison.Ordinal));
            Assert.Equal(part, Pdfaid(pdf, "part"));
            Assert.Equal(level, Pdfaid(pdf, "conformance"));
            Assert.Equal(subtype, EmbeddedFileSubtype(pdf));
            Assert.Equal(desc, FilespecDescription(pdf));
            var attachmentWarnings = doc.Diagnostics.Where(d => d.Category == DiagnosticCategory.Attachment).ToList();
            Assert.Equal(warnings, attachmentWarnings.Count);
            if (warning != null)
            {
                Assert.Equal(DiagnosticSeverity.Warning, attachmentWarnings[0].Severity);
                Assert.Equal(warning, attachmentWarnings[0].Message);
                Assert.Equal("data.xml", attachmentWarnings[0].Source);
            }
        }
    }

    [Fact]
    public void PdfUa2_WarnsOncePerAttachmentDescribedByFileName_InFileNameOrder()
    {
        var (_, doc) = Generate(doc =>
        {
            doc.Settings.Conformance = PdfAConformance.PdfA4f;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa2;
            doc.Add(new TextBlock("Attachment guard"));
            doc.Attach("b.csv", Encoding.UTF8.GetBytes("1"));
            doc.Attach("a.csv", Encoding.UTF8.GetBytes("2"));
            doc.Attach(new FileAttachment { FileName = "c.csv", Content = Encoding.UTF8.GetBytes("3"), Description = "Totals" });
            doc.Attach("empty.csv", System.Array.Empty<byte>());
        });
        using (doc)
        {
            Assert.Equal(new[] { "a.csv", "b.csv" },
                doc.Diagnostics.Where(d => d.Category == DiagnosticCategory.Attachment).Select(d => d.Source),
                StringComparer.Ordinal);
        }
    }

    [Fact]
    public void PdfA4f_WithoutAttachment_Throws()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA4f;
        doc.Add(new TextBlock("Attachment guard"));
        var ex = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.StartsWith("PdfA4f requires at least one attachment.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA4f_WithOnlyAnEmptyAttachment_Throws()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA4f;
        doc.Add(new TextBlock("Attachment guard"));
        doc.Attach("empty.bin", System.Array.Empty<byte>());
        Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
    }

    /// <summary>The value of an XMP <c>pdfaid</c> property, or null when it is absent.</summary>
    private static string? Pdfaid(string pdf, string property)
    {
        int start = pdf.IndexOf("<pdfaid:" + property, StringComparison.Ordinal);
        if (start < 0) return null;
        int open = pdf.IndexOf('>', start) + 1;
        int close = pdf.IndexOf('<', open);
        return pdf[open..close];
    }

    /// <summary>The <c>/Desc</c> of the first file specification, or null.</summary>
    private static string? FilespecDescription(string pdf)
    {
        int start = pdf.IndexOf("/Type /Filespec", StringComparison.Ordinal);
        if (start < 0) return null;
        int key = pdf.IndexOf("/Desc (", start, StringComparison.Ordinal);
        int end = pdf.IndexOf("endobj", start, StringComparison.Ordinal);
        if (key < 0 || key > end) return null;
        int open = key + "/Desc (".Length;
        return pdf[open..pdf.IndexOf(')', open)];
    }

    /// <summary>The <c>/Subtype</c> of the embedded-file stream's dictionary, or null.</summary>
    private static string? EmbeddedFileSubtype(string pdf)
    {
        int start = pdf.IndexOf("/Type /EmbeddedFile", StringComparison.Ordinal);
        if (start < 0) return null;
        string dict = pdf[start..pdf.IndexOf("endstream", start, StringComparison.Ordinal)];
        int key = dict.IndexOf("/Subtype /", StringComparison.Ordinal);
        if (key < 0) return null;
        int value = key + "/Subtype ".Length;
        int end = dict.IndexOfAny([' ', '\n', '\r', '>'], value + 1);
        return dict[value..end];
    }

    [Fact]
    public void IgnoresEmptyAttachments()
    {
        var (pdf, doc) = Generate(doc =>
        {
            doc.Add(new TextBlock("hello"));
            doc.Attach("empty.bin", System.Array.Empty<byte>());
        });
        using (doc)
        {
            Assert.DoesNotContain("/Type /EmbeddedFile", pdf);
        }
    }
}
