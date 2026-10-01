using System.Text;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Elements;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Layout;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// PDF/UA-1 7.1 marked-content nesting for elements that draw onto a canvas
/// obtained from <c>RenderContext.GetCanvas</c> (charts, canvas elements).
///
/// Regression: that call hands back a *fresh* canvas positioned at the child's
/// origin, but it appends to the same content stream as the canvas already
/// rendering. The new canvas used to start with its scope counters at zero, so
/// a chart drawn inside the `/Figure` BDC its parent had opened wrapped its own
/// marks in `/Artifact BMC` — an artifact nested inside tagged content, which
/// veraPDF rejects under ISO 14289-1 7.1 tests 1 and 2. Every tagged document
/// containing a chart was affected, including ones claiming PDF/UA-1.
/// </summary>
public class ChartTaggingTests
{
    private static ChartData SampleData() => new()
    {
        Series = [new ChartSeries { DataPoints = [
            new ChartDataPoint { Label = "A", Value = 3 },
            new ChartDataPoint { Label = "B", Value = 5 },
        ] }],
    };

    private static byte[] RenderTagged(Action<BonniePDF.Document> populate)
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);

        using MemoryStream ms = new();
        doc.Save(ms, true);
        return ms.ToArray();
    }

    /// <summary>
    /// Content streams are Flate-compressed, so the marked-content operators are
    /// not visible in the raw bytes. Inflating them is what makes these
    /// assertions meaningful rather than vacuously true.
    /// </summary>
    private static string ContentStreams(byte[] pdf)
    {
        StringBuilder all = new();
        int i = 0;
        while (true)
        {
            int start = IndexOf(pdf, "stream"u8, i);
            if (start < 0) break;
            int data = start + 6;
            if (data < pdf.Length && pdf[data] == '\r') data++;
            if (data < pdf.Length && pdf[data] == '\n') data++;
            int end = IndexOf(pdf, "endstream"u8, data);
            if (end < 0) break;

            try
            {
                using MemoryStream raw = new(pdf, data, end - data);
                using System.IO.Compression.ZLibStream inflate = new(raw, System.IO.Compression.CompressionMode.Decompress);
                using MemoryStream outMs = new();
                inflate.CopyTo(outMs);
                all.Append(Encoding.Latin1.GetString(outMs.ToArray())).Append('\n');
            }
            catch (InvalidDataException)
            {
                // Not a Flate stream (an image, a font file): not content.
            }

            i = end + 9;
        }
        return all.ToString();
    }

    private static int IndexOf(byte[] haystack, ReadOnlySpan<byte> needle, int from)
    {
        for (int i = from; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle)) return i;
        }
        return -1;
    }

    /// <summary>
    /// The precise defect: `/Artifact BMC` appearing between a `BDC` and its
    /// `EMC`. Scans the stream in order and fails if an artifact opens while a
    /// tagged scope is still open.
    /// </summary>
    private static void AssertNoArtifactInsideTaggedContent(string pdf)
    {
        int taggedDepth = 0;
        foreach (string rawLine in pdf.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.EndsWith("BDC", StringComparison.Ordinal) && line.Contains("/MCID", StringComparison.Ordinal))
            {
                taggedDepth++;
            }
            else if (line == "/Artifact BMC")
            {
                Assert.True(taggedDepth == 0,
                    "An /Artifact region was opened inside tagged content, which PDF/UA-1 7.1 forbids.");
            }
            else if (line == "EMC" && taggedDepth > 0)
            {
                taggedDepth--;
            }
        }
    }

    /// <summary>
    /// Wraps an element the way real documents do — inside a container that
    /// renders its own children. That path goes through
    /// <c>ChildTagging.BeginChildTag</c>, which opens the scope on the *parent*
    /// canvas, while the chart then draws on one from <c>GetCanvas</c>. A chart
    /// added straight to the page does not reproduce the defect.
    /// </summary>
    private static Container Nested(IElement child)
    {
        VerticalStack stack = new();
        stack.Add(child);
        return new Container { Child = stack };
    }

    [Fact]
    public void ChartWithAltText_DoesNotNestAnArtifactInsideItsFigure()
    {
        string pdf = ContentStreams(RenderTagged(doc => doc.Add(Nested(new BarChart
        {
            Data = SampleData(),
            AltText = "Bar chart of two sample values.",
            Height = 120,
            // The background fill is what reaches the artifact fallback; without it
            // the chart draws nothing that would have been wrapped.
            BackgroundColor = BonniePDF.Core.Color.White,
        }))));

        Assert.Contains("/Figure", pdf, StringComparison.Ordinal);
        AssertNoArtifactInsideTaggedContent(pdf);
    }

    [Fact]
    public void DecorativeChart_DoesNotNestAnArtifactInsideItsFigure()
    {
        string pdf = ContentStreams(RenderTagged(doc => doc.Add(Nested(new BarChart
        {
            Data = SampleData(),
            IsDecorative = true,
            Height = 120,
            BackgroundColor = BonniePDF.Core.Color.White,
        }))));

        AssertNoArtifactInsideTaggedContent(pdf);
    }

    /// <summary>
    /// A canvas element takes the same `GetCanvas` path a chart does, so it has
    /// to inherit the enclosing scope too.
    /// </summary>
    [Fact]
    public void CanvasElementWithAltText_DoesNotNestAnArtifactInsideItsFigure()
    {
        string pdf = ContentStreams(RenderTagged(doc => doc.Add(Nested(new CanvasElement((canvas, width, height) =>
            canvas.FillRect(0, 0, width, height, BonniePDF.Core.ColorPalette.Slate[200]))
        {
            AltText = "A filled sample rectangle.",
            DrawHeight = 40,
        }))));

        AssertNoArtifactInsideTaggedContent(pdf);
    }
}
