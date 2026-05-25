using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Metadata;
using Xunit;

namespace BonniePDF.Tests;

public class XmpMetadataTests
{
    [Fact]
    public void Pdf17_ContainsXmpMetadata()
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Metadata.Title = "Test Document";
        doc.Metadata.Author = "Test Author";

        using MemoryStream ms = new();
        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("/Metadata", content);
        Assert.Contains("/Subtype /XML", content);
    }

    [Fact]
    public void Pdf14_DoesNotContainXmpMetadata()
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf14;

        using MemoryStream ms = new();
        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.DoesNotContain("/Subtype /XML", content);
    }

    [Fact]
    public void PdfA_ContainsPdfAIdentification()
    {
        using Document doc = new();
        doc.Settings.AsPdfA();
        doc.Metadata.Title = "Archival Document";

        using MemoryStream ms = new();
        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("pdfaid:part", content);
        Assert.Contains("pdfaid:conformance", content);
    }

    [Fact]
    public void XmpMetadataWriter_GeneratesValidXml()
    {
        var metadata = new DocumentMetadata
        {
            Title = "My Title",
            Author = "Jane Doe",
            Subject = "Testing",
            Keywords = "pdf, test, xmp"
        };

        string xml = XmpMetadataWriter.GenerateXmpXml(metadata);

        Assert.Contains("<?xpacket begin=", xml);
        Assert.Contains("<?xpacket end=", xml);
        Assert.Contains("My Title", xml);
        Assert.Contains("Jane Doe", xml);
        Assert.Contains("Testing", xml);
        Assert.Contains("pdf", xml);
    }

    [Fact]
    public void XmpMetadataWriter_PdfA_IncludesConformance()
    {
        var metadata = new DocumentMetadata { Title = "Test" };
        string xml = XmpMetadataWriter.GenerateXmpXml(metadata, new[] { XmpConformanceBlock.PdfA1B() });

        Assert.Contains("pdfaid:part", xml);
        Assert.Contains("1", xml);
        Assert.Contains("pdfaid:conformance", xml);
        Assert.Contains("B", xml);
    }

    // ── PDF/UA conformance and additive composition ──────────

    [Fact]
    public void XmpMetadataWriter_PdfUa1_IncludesPdfuaidPart()
    {
        var metadata = new DocumentMetadata { Title = "Accessibility test" };
        string xml = XmpMetadataWriter.GenerateXmpXml(metadata, new[] { XmpConformanceBlock.PdfUa1() });

        Assert.Contains("xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"", xml);
        Assert.Contains("<pdfuaid:part", xml);
        Assert.Contains(">1<", xml);
    }

    [Fact]
    public void XmpMetadataWriter_NoConformance_OmitsPdfuaidAndPdfaid()
    {
        var metadata = new DocumentMetadata { Title = "Plain document" };
        string xml = XmpMetadataWriter.GenerateXmpXml(metadata);

        Assert.DoesNotContain("pdfuaid", xml);
        Assert.DoesNotContain("pdfaid", xml);
    }

    [Fact]
    public void XmpMetadataWriter_PdfA_PlusPdfUa1_ComposesBothNamespaces()
    {
        // The additive-hook contract: two registered blocks emit into the
        // same XMP packet without collision. This is the future-proofing
        // smoke test promised by the this feature this feature design — when ZUGFeRD lands
        // it'll be a third entry in this same array.
        var metadata = new DocumentMetadata { Title = "Both" };
        string xml = XmpMetadataWriter.GenerateXmpXml(metadata, new[]
        {
            XmpConformanceBlock.PdfA1B(),
            XmpConformanceBlock.PdfUa1(),
        });

        Assert.Contains("pdfaid:part", xml);
        Assert.Contains("pdfaid:conformance", xml);
        Assert.Contains("pdfuaid:part", xml);
        // The output is well-formed XML — namespaces don't collide and
        // we're not double-declaring xmlns attributes.
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml);
        Assert.NotNull(doc.DocumentElement);
    }
}
