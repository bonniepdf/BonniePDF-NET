using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Security.Models;
using Xunit;

namespace BonniePDF.Tests.Export;

public class PdfVersionTests
{
    [Fact]
    public void DefaultVersionIsPdf14()
    {
        using BonniePDF.Document doc = new();
        Assert.Equal(PdfVersion.Pdf14, doc.Settings.Version);
    }

    [Fact]
    public void Pdf14_OutputStartsWithCorrectHeader()
    {
        using BonniePDF.Document doc = new();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string line = reader.ReadLine();
        Assert.StartsWith("%PDF-1.4", line);
    }

    [Fact]
    public void Pdf17_OutputStartsWithCorrectHeader()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        using MemoryStream ms = new();

        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string line = reader.ReadLine();
        Assert.StartsWith("%PDF-1.7", line);
    }

    [Fact]
    public void AsPdf17_SetsVersion()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdf17();
        Assert.Equal(PdfVersion.Pdf17, doc.Settings.Version);
    }

    [Fact]
    public void AsPdfA_SetsVersionAndFlag()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA();
        Assert.Equal(PdfVersion.Pdf17, doc.Settings.Version);
        Assert.True(doc.Settings.PdfAEnabled);
    }

    [Fact]
    public void PdfA_CanSaveToStream()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("/OutputIntents", content);
        Assert.Contains("/GTS_PDFA1", content);
    }

    [Fact]
    public void PdfA_WithEncryption_ThrowsInvalidOperation()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA();
        doc.Settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.RC4_128,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        using MemoryStream ms = new();
        Assert.Throws<InvalidOperationException>(() => doc.Save(ms, true));
    }

    [Fact]
    public void AsPdf20_SetsVersionWithoutConformanceClaims()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdf20();
        Assert.Equal(PdfVersion.Pdf20, doc.Settings.Version);
        Assert.False(doc.Settings.PdfAEnabled);
    }

    [Fact]
    public void AsPdfA4_SetsConformanceAndVersion()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA4();
        Assert.Equal(PdfVersion.Pdf20, doc.Settings.Version);
        Assert.Equal(PdfAConformance.PdfA4, doc.Settings.Conformance);
        Assert.True(doc.Settings.PdfAEnabled);
    }

    [Fact]
    public void Pdf20_OutputStartsWithCorrectHeader()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdf20();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string line = reader.ReadLine();
        Assert.StartsWith("%PDF-2.0", line);
    }

    [Fact]
    public void Pdf20_IncludesMetadataWithoutConformanceClaims()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdf20();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("/Metadata", content);
        Assert.DoesNotContain("pdfaid:part", content);
        Assert.DoesNotContain("pdfuaid:part", content);
    }

    [Fact]
    public void Pdf20_WithPdfUa1_Throws()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdf20();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        using MemoryStream ms = new();

        Assert.Throws<InvalidOperationException>(() => doc.Save(ms, true));
    }

    [Fact]
    public void PdfA4_DirectConformanceAutoUpgradesToPdf20()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA4;
        using MemoryStream ms = new();

        doc.Save(ms, true);

        Assert.Equal(PdfVersion.Pdf20, doc.Settings.Version);
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string line = reader.ReadLine();
        Assert.StartsWith("%PDF-2.0", line);
    }

    [Fact]
    public void PdfA4_OmitsLegacyInfoDictionary()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA4();
        doc.Metadata.Title = "XMP-only PDF/A-4 metadata";
        using MemoryStream ms = new();

        doc.Save(ms, true);

        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.DoesNotContain("/Info ", content, StringComparison.Ordinal);
        Assert.Contains("XMP-only PDF/A-4 metadata", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA4_WithEncryption_ThrowsInvalidOperation()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AsPdfA4();
        doc.Settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.RC4_128,
            UserPassword = "test",
            OwnerPassword = "test"
        };

        using MemoryStream ms = new();
        Assert.Throws<InvalidOperationException>(() => doc.Save(ms, true));
    }
}
