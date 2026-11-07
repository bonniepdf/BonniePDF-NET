using BonniePDF;
using Align = BonniePDF.TextAlignment;

namespace BonniePDF.Examples.Fonts.EmbeddedFontMetrics;

/// <summary>
/// Embedded-font metrics — why an embedded font makes alignment edges exact.
///
/// The standard-14 Type1 fonts are measured from approximate AFM width tables. An
/// embedded TrueType font instead carries its own <c>hmtx</c> advance widths, so
/// BonniePDF measures and places every glyph from the real metrics. That precision
/// is what makes right, centre, and justified edges land exactly on the margin.
///
/// This example renders the four alignments entirely in embedded Liberation Sans
/// (embedded as compact glyph subsets). PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Embedded-Font Metrics")]
[System.ComponentModel.Category("Fonts")]
[System.ComponentModel.Description(
    "Alignment measured from the embedded font's own advance widths: four framed samples whose rules mark the measure, so every edge can be seen landing on them. PDF/A-2a + PDF/UA-1.")]
public static class EmbeddedFontMetricsExample
{
    private const string SampleText =
        "The quick brown fox jumps over the lazy dog. Every line here is measured from the advance " +
        "widths stored in the embedded font, the same font that draws it, so the edge this alignment " +
        "promises lands on the rule beside it in any viewer.";

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        string sansPath = Path.Combine(AppContext.BaseDirectory, "fonts", "LiberationSans-Regular.ttf");
        string sansBoldPath = Path.Combine(AppContext.BaseDirectory, "fonts", "LiberationSans-Bold.ttf");

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Embedded-Font Metrics",
                Author = "BonniePDF",
                Subject =
                    "How an embedded TrueType font makes alignment edges exact, using its own hmtx advance widths.",
                Keywords = "pdf, pdf/ua, pdf/a, fonts, embedding, metrics, hmtx, alignment, truetype, liberation",
            },
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 60,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(sansPath)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        doc.Add(new Heading("Embedded-Font Metrics")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = sansBoldPath,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new TextBlock
        {
            Text =
                "An embedded font carries its own advance widths, and the same font program draws the text, so the widths " +
                "measured during layout are the widths every reader sees. Each frame below marks the measure with a rule on " +
                "each side; the edges each alignment promises land on them.",
            LineHeight = 1.45f,
            Margin = new Thickness(0, 0, 0, 18),
        });

        doc.Add(Row(FramedSample("Left", "left edge on the rule", Align.Left, sansBoldPath), FramedSample("Centre", "centred between the rules", Align.Center, sansBoldPath), 18));
        doc.Add(Row(FramedSample("Right", "right edge on the rule", Align.Right, sansBoldPath), FramedSample("Justified", "both edges on the rules", Align.Justify, sansBoldPath), 24));

        // ── Book-style justified paragraphs, framed the same way ──────────────
        doc.Add(new Heading("Book-style justified paragraphs")
        {
            TocIdentifier = "book",
            Level = 2,
            FontName = sansBoldPath,
            FontSize = 13,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 8),
        });

        string[] paragraphs =
        {
            "In the beginning, there was only the vast emptiness of space, stretching infinitely in all directions. " +
            "Then, from the primordial void, matter began to coalesce under the inexorable pull of gravity, forming " +
            "the first stars that would illuminate the cosmos.",

            "These ancient stars burned with a fierce intensity, forging heavier elements in their cores through " +
            "nuclear fusion. Carbon, oxygen, iron, and gold were all born in the hearts of dying stars, scattered " +
            "across the universe in spectacular supernova explosions.",

            "From this cosmic debris, new generations of stars formed, surrounded by disks of dust and gas that " +
            "would eventually coalesce into planets. On at least one of these worlds, conditions were just right " +
            "for something remarkable to happen: the emergence of life.",
        };

        VerticalStack book = new() { Spacing = 8 };
        foreach (string p in paragraphs)
            book.Add(new TextBlock(p) { FontName = sansPath, Alignment = Align.Justify, LineHeight = 1.4f });
        doc.Add(Frame(book));

        return doc;
    }

    /// <summary>Two framed samples side by side.</summary>
    private static HorizontalStack Row(VerticalStack left, VerticalStack right, float marginBottom)
    {
        HorizontalStack stack = new() { Spacing = 24, WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, marginBottom) };
        stack.Add(left);
        stack.Add(right);
        return stack;
    }

    /// <summary>A frame whose left and right rules mark the measure, with no padding between rule and text.</summary>
    private static Container Frame(IElement child) => new()
    {
        Border = new Border(0.75f, 0, 0.75f, 0),
        BorderColors = new BorderColors(ColorPalette.Teal[500]),
        BackgroundColor = ColorPalette.Slate[50],
        Padding = new Thickness(0, 8, 0, 8),
        Child = child,
    };

    /// <summary>One alignment: a heading, a caption saying what to look for, and the framed sample.</summary>
    private static VerticalStack FramedSample(string title, string caption, Align alignment, string boldPath)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new Heading(title) { Level = 2, FontName = boldPath, FontSize = 12, Color = ColorPalette.Slate[900] });
        stack.Add(new TextBlock(caption) { FontSize = 9, Color = ColorPalette.Teal[700] });
        stack.Add(Frame(new TextBlock(SampleText) { Alignment = alignment, FontSize = 10.5f, LineHeight = 1.4f }));
        return stack;
    }
}
