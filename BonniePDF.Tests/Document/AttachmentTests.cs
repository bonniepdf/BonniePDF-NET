using System.Text;
using BonniePDF.Attachments;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// file attachments (embedded / associated files). Parity peer of the
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
    [InlineData(PdfAConformance.PdfA4, true)]
    [InlineData(PdfAConformance.PdfA1b, false)]
    [InlineData(PdfAConformance.PdfA1a, false)]
    [InlineData(PdfAConformance.PdfA2b, false)]
    [InlineData(PdfAConformance.PdfA2a, false)]
    public void AttachmentRules_MatchesSpec(PdfAConformance conformance, bool allowed)
    {
        Assert.Equal(allowed, AttachmentRules.AllowsEmbeddedFiles(conformance));
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
