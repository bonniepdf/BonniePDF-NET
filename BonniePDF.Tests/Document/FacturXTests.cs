using System.Text;
using BonniePDF.Configuration;
using BonniePDF.EInvoice;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// ZUGFeRD / Factur-X e-invoice binding. Parity peer of the TypeScript
/// <c>tests/document/factur-x.test.ts</c>. Asserts the PDF structure (PDF/A-3
/// + embedded XML + fx: XMP); real conformance still requires veraPDF and an
/// independent ZUGFeRD validator, which run in the validation suite.
/// </summary>
[Collection("Serial")]
public class FacturXTests
{
    public FacturXTests()
    {
        // The fx block only survives when the PDF/A-3 claim survives, which
        // requires embeddable fonts (PDF/A forces standard-14 embedding).
        // Restore bundled Liberation fonts in case a prior serial test cleared them.
        FontManager.RestoreBundledFonts();
    }

    private static byte[] InvoiceXml() =>
        Encoding.UTF8.GetBytes("<rsm:CrossIndustryInvoice><total>42.00</total></rsm:CrossIndustryInvoice>");

    private static string Generate(System.Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    [Fact]
    public void Apply_ForcesPdfA3_AttachesXml_AndWritesFxXmp()
    {
        string pdf = Generate(doc =>
        {
            doc.Add(new TextBlock("Invoice #1001"));
            FacturX.Apply(doc, InvoiceXml(), FacturXProfile.En16931);
        });

        // PDF/A-3 claim survived the honest-claim guard.
        Assert.Contains("pdfaid:part", pdf);
        // Embedded XML as an associated file.
        Assert.Contains("/Type /EmbeddedFile", pdf);
        Assert.Contains("/AFRelationship /Data", pdf);
        Assert.Contains("(factur-x.xml)", pdf);
        // fx: XMP block + values.
        Assert.Contains("urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#", pdf);
        Assert.Contains("EN 16931", pdf);
        Assert.Contains("factur-x.xml", pdf);
        Assert.Contains("INVOICE", pdf);
        // fx namespace is non-predefined -> extension schema bag present.
        Assert.Contains("Factur-X PDFA Extension Schema", pdf);
    }

    [Fact]
    public void Apply_KeepsExistingPdfA3aChoice()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA3a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        FacturX.Apply(doc, InvoiceXml(), FacturXProfile.Basic);
        Assert.Equal(PdfAConformance.PdfA3a, doc.Settings.Conformance);
        doc.Dispose();
    }

    [Theory]
    [InlineData(FacturXProfile.Minimum, "MINIMUM")]
    [InlineData(FacturXProfile.BasicWl, "BASIC WL")]
    [InlineData(FacturXProfile.Basic, "BASIC")]
    [InlineData(FacturXProfile.En16931, "EN 16931")]
    [InlineData(FacturXProfile.Extended, "EXTENDED")]
    [InlineData(FacturXProfile.XRechnung, "XRECHNUNG")]
    public void Profile_MapsToConformanceLevel(FacturXProfile profile, string expected)
    {
        Assert.Equal(expected, profile.ToConformanceLevel());
    }

    [Fact]
    public void Apply_RejectsEmptyXml()
    {
        using var doc = new BonniePDF.Document();
        Assert.Throws<System.ArgumentException>(() => FacturX.Apply(doc, System.Array.Empty<byte>()));
    }

    [Fact]
    public void CustomFileName_FlowsToAttachmentAndXmp()
    {
        string pdf = Generate(doc =>
        {
            doc.Add(new TextBlock("ZUGFeRD"));
            FacturX.Apply(doc, InvoiceXml(), FacturXProfile.En16931, fileName: "zugferd-invoice.xml");
        });
        Assert.Contains("(zugferd-invoice.xml)", pdf);
        Assert.DoesNotContain("(factur-x.xml)", pdf);
    }
}
