using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.TextDecorations;

/// <summary>
/// Underline and strikethrough decorations across alignments and sizes.
/// PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Text Decorations")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Underline and strikethrough decorations across alignments and font sizes, with a practical price-list example. PDF/A-2a + PDF/UA-1.")]
public static class TextDecorationExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Text Decorations",
                Author   = "BonniePDF",
                Subject  = "Underline and strikethrough decorations across alignments and sizes. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, text, underline, strikethrough, decorations, pdf/ua, pdf/a, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 60,
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(12)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Text Decorations")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "Underline and strikethrough are presentational properties on TextBlock. " +
                     "They do not change how the text is tagged — a decorated paragraph is still a paragraph.",
            Margin = new Thickness(0, 0, 0, 14),
        });

        // ── 1. Underline ───────────────────────────────────────────────────────

        Section(doc, "Underline", "s-underline");

        doc.Add(new TextBlock
        {
            Text           = "This text has an underline applied. Underlines can mark emphasis, hyperlinks, or headings in informal documents.",
            TextDecoration = TextDecoration.Underline,
            Margin         = new Thickness(0, 0, 0, 10),
        });

        // ── 2. Strikethrough ───────────────────────────────────────────────────

        Section(doc, "Strikethrough", "s-strike");

        doc.Add(new TextBlock
        {
            Text           = "This text has a strikethrough applied. Strikethrough visually signals content that has been withdrawn, revised, or superseded.",
            TextDecoration = TextDecoration.Strikethrough,
            Margin         = new Thickness(0, 0, 0, 10),
        });

        // ── 3. Combined ────────────────────────────────────────────────────────

        Section(doc, "Combined Decorations", "s-combined");

        doc.Add(new TextBlock
        {
            Text           = "Both decorations can be applied simultaneously. TextDecoration is a set of flags — combine them with the | operator: " +
                             "TextDecoration.Underline | TextDecoration.Strikethrough.",
            TextDecoration = TextDecoration.Underline | TextDecoration.Strikethrough,
            Margin         = new Thickness(0, 0, 0, 10),
        });

        // ── 4. Alignment ───────────────────────────────────────────────────────

        Section(doc, "Alignment", "s-alignment");

        (string Text, TextAlignment Align)[] alignments =
        [
            ("Left-aligned underlined text.",   TextAlignment.Left),
            ("Centre-aligned underlined text.", TextAlignment.Center),
            ("Right-aligned underlined text.",  TextAlignment.Right),
        ];

        foreach (var (text, align) in alignments)
        {
            doc.Add(new TextBlock
            {
                Text           = text,
                TextDecoration = TextDecoration.Underline,
                Alignment      = align,
                Margin         = new Thickness(0, 0, 0, 8),
            });
        }

        // ── 5. Scales with font size ───────────────────────────────────────────

        Section(doc, "Scales with Font Size", "s-sizes");

        foreach (int size in new[] { 8, 12, 18, 24, 36 })
        {
            doc.Add(new TextBlock
            {
                Text           = $"Underline at {size}pt",
                FontSize       = size,
                TextDecoration = TextDecoration.Underline,
                Margin         = new Thickness(0, 0, 0, 6),
            });
        }

        // ── Practical Example ──────────────────────────────────────────────────

        doc.Add(new Line
        {
            Color     = ColorPalette.Slate[300],
            Thickness = 1,
            Margin    = new Thickness(0, 12, 0, 0),
        });

        Section(doc, "Practical Example: Price List", "s-prices");

        doc.Add(new TextBlock
        {
            Text           = "Original Price: £199.99",
            FontSize       = 14,
            // 600, not 400: 400 never clears 4.5:1 as text (2.63:1 here).
            TextColor      = ColorPalette.Slate[600],
            TextDecoration = TextDecoration.Strikethrough,
            Margin         = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text           = "Sale Price: £149.99",
            FontName       = StandardFonts.HelveticaBold,
            FontSize       = 18,
            TextColor      = Color.Red,
            TextDecoration = TextDecoration.Underline,
            Margin         = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text      = "You save £50.00!",
            FontSize  = 11,
            // 700, not 600: 11pt regular text on white needs 4.5:1 and 600 falls
            // to 3.22:1.
            TextColor = ColorPalette.Green[700],
        });

        return doc;
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 12, 0, 6),
        });
    }
}
