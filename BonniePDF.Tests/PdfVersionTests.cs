using BonniePDF.Configuration;
using BonniePDF.Extensions;
using BonniePDF.Security.Models;
using Xunit;

namespace BonniePDF.Tests;

public class PdfVersionTests
{
    [Fact]
    public void DefaultVersionIsPdf14()
    {
        using Document doc = new();
        Assert.Equal(PdfVersion.Pdf14, doc.Settings.Version);
    }

    [Fact]
    public void Pdf14_OutputStartsWithCorrectHeader()
    {
        using Document doc = new();
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
        using Document doc = new();
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
        using Document doc = new();
        doc.Settings.AsPdf17();
        Assert.Equal(PdfVersion.Pdf17, doc.Settings.Version);
    }

    [Fact]
    public void AsPdfA_SetsVersionAndFlag()
    {
        using Document doc = new();
        doc.Settings.AsPdfA();
        Assert.Equal(PdfVersion.Pdf17, doc.Settings.Version);
        Assert.True(doc.Settings.PdfAEnabled);
    }

    [Fact]
    public void PdfA_CanSaveToStream()
    {
        using Document doc = new();
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
        using Document doc = new();
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
}
