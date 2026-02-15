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

        string xml = XmpMetadataWriter.GenerateXmpXml(metadata, false);

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
        string xml = XmpMetadataWriter.GenerateXmpXml(metadata, true);

        Assert.Contains("<pdfaid:part>1</pdfaid:part>", xml);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", xml);
    }
}
