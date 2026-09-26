using System.IO.Compression;
using System.Text.RegularExpressions;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// The document-level behaviour of the image resolution cap: what is embedded, at what size,
/// once or twice, and what is reported. The resampler itself is covered by
/// <see cref="ImageResamplerTests"/>. Mirrors core/tests/drawing/image-resolution-cap.test.ts.
/// </summary>
public partial class ImageResolutionCapTests
{
    private sealed record EmbeddedImage(int Width, int Height, bool HasSoftMask, byte[] Pixels);

    [GeneratedRegex(@"\d+ 0 obj\s*<<([^>]*?/Subtype\s*/Image[^>]*?)>>\s*stream\r?\n")]
    private static partial Regex ImageObject();

    [GeneratedRegex(@"/Length (\d+)")]
    private static partial Regex Length();

    [GeneratedRegex(@"/Width (\d+)")]
    private static partial Regex Width();

    [GeneratedRegex(@"/Height (\d+)")]
    private static partial Regex Height();

    // The image XObjects in a PDF, with their stream contents inflated.
    private static List<EmbeddedImage> EmbeddedImages(byte[] pdf)
    {
        string text = System.Text.Encoding.Latin1.GetString(pdf);
        var images = new List<EmbeddedImage>();
        foreach (Match m in ImageObject().Matches(text))
        {
            string dict = m.Groups[1].Value;
            int length = int.Parse(Length().Match(dict).Groups[1].Value);
            int width = int.Parse(Width().Match(dict).Groups[1].Value);
            int height = int.Parse(Height().Match(dict).Groups[1].Value);
            using var inflated = new MemoryStream();
            using (var z = new ZLibStream(new MemoryStream(pdf, m.Index + m.Length, length), CompressionMode.Decompress))
                z.CopyTo(inflated);
            images.Add(new EmbeddedImage(width, height, dict.Contains("/SMask"), inflated.ToArray()));
        }
        return images;
    }

    private static PdfDocument DocumentWith(int? maxDpi, params Image[] images)
    {
        var doc = new PdfDocument(new DocumentSettings { MaxImageDpi = maxDpi });
        foreach (Image image in images) doc.Add(image);
        return doc;
    }

    private static Image Png(int width, int height, float drawnWidth, bool keep = false) => new()
    {
        ImageData = ImageLoaderTests.BuildFilteredRgbaPng(width, height, _ => 0),
        Width = drawnWidth,
        KeepResolution = keep,
        AltText = "test image",
    };

    [Fact]
    public void AnOversampledPng_IsReducedToTheCap()
    {
        using PdfDocument doc = DocumentWith(288, Png(1200, 600, 75));

        var embedded = EmbeddedImages(doc.GenerateBytes());

        // 75 pt at 288 ppi is 300 px; the height follows the aspect ratio: 37.5 pt is 150 px.
        Assert.All(embedded, i => { Assert.Equal(300, i.Width); Assert.Equal(150, i.Height); });
        Assert.Equal(2, embedded.Count); // the colour plane and its soft mask
    }

    [Fact]
    public void NullMaxImageDpi_EmbedsTheSourcePixels()
    {
        using PdfDocument doc = DocumentWith(null, Png(1200, 600, 75));

        Assert.All(EmbeddedImages(doc.GenerateBytes()), i => { Assert.Equal(1200, i.Width); Assert.Equal(600, i.Height); });
    }

    [Fact]
    public void KeepResolution_EmbedsTheSourcePixels()
    {
        using PdfDocument doc = DocumentWith(288, Png(1200, 600, 75, keep: true));

        Assert.All(EmbeddedImages(doc.GenerateBytes()), i => Assert.Equal(1200, i.Width));
    }

    [Fact]
    public void AnImageUsedTwice_IsEmbeddedOnce_SizedForItsLargestUse()
    {
        Image big = Png(1200, 600, 150);
        Image small = Png(1200, 600, 75); // the same source bytes, drawn smaller
        small.ImageData = big.ImageData;
        using PdfDocument doc = DocumentWith(288, small, big);

        var embedded = EmbeddedImages(doc.GenerateBytes());

        Assert.Equal(2, embedded.Count); // one colour plane, one mask - not two of each
        Assert.All(embedded, i => Assert.Equal(600, i.Width)); // 150 pt at 288 ppi
    }

    [Fact]
    public void OneKeepResolutionPlacement_KeepsTheSharedImageWhole()
    {
        Image kept = Png(1200, 600, 75, keep: true);
        Image small = Png(1200, 600, 75);
        small.ImageData = kept.ImageData;
        using PdfDocument doc = DocumentWith(288, small, kept);

        Assert.All(EmbeddedImages(doc.GenerateBytes()), i => Assert.Equal(1200, i.Width));
    }

    [Fact]
    public void AnImageInsideTheTolerance_IsLeftAlone()
    {
        // 1200 x 960 drawn 250 pt wide: a 1000 x 800 target, 1.44x the pixels - under the 1.5x threshold.
        using PdfDocument doc = DocumentWith(288, Png(1200, 960, 250));

        Assert.All(EmbeddedImages(doc.GenerateBytes()), i => Assert.Equal(1200, i.Width));
    }

    [Fact]
    public void AnImageBelowTheCap_IsNeverEnlarged()
    {
        using PdfDocument doc = DocumentWith(288, Png(100, 50, 300));

        Assert.All(EmbeddedImages(doc.GenerateBytes()), i => Assert.Equal(100, i.Width));
    }

    [Fact]
    public void TheSyncAndAsyncPaths_EmbedIdenticalPixels()
    {
        using PdfDocument sync = DocumentWith(288, Png(1200, 600, 75));
        using PdfDocument async = DocumentWith(288, Png(1200, 600, 75));

        var a = EmbeddedImages(sync.GenerateBytes());
        var b = EmbeddedImages(async.GenerateBytesAsync().GetAwaiter().GetResult());

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Width, b[i].Width);
            Assert.True(a[i].Pixels.AsSpan().SequenceEqual(b[i].Pixels), "reduced pixels differ between the sync and async paths");
        }
    }

    [Fact]
    public void AResampledImage_IsReportedInTheDiagnostics()
    {
        using PdfDocument doc = DocumentWith(288, Png(1200, 600, 75));
        doc.GenerateBytes();

        Diagnostic d = Assert.Single(doc.Diagnostics, x => x.Category == DiagnosticCategory.Image);

        Assert.Equal(DiagnosticSeverity.Info, d.Severity);
        Assert.StartsWith("image.resampled: 1200x600 -> 300x150", d.Message);
    }

    [Fact]
    public void AJpeg_IsPassedThrough_AndSaysSo()
    {
        var jpeg = new Image { ImageData = ImageLoaderTests.CreateMinimalJpeg(), Width = 5, AltText = "test image" };
        using PdfDocument doc = DocumentWith(288, jpeg);

        doc.GenerateBytes();

        Diagnostic d = Assert.Single(doc.Diagnostics, x => x.Category == DiagnosticCategory.Image);
        Assert.StartsWith("image.passed-through:", d.Message);
        Assert.Contains("not a PNG", d.Message);
    }

    [Fact]
    public void MaxImageDpi_RejectsANonPositiveValue() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentSettings { MaxImageDpi = 0 });
}
