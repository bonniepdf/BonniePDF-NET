using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.RichTextStyles;

/// <summary>
/// Bold, italic, coloured, highlighted, and mixed-size spans within flowing paragraphs.
/// Reusable style objects, justified alignment, and a practical legal disclaimer.
/// PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Rich Text Styles")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Bold, italic, coloured, highlighted, and mixed-size spans within flowing paragraphs. Reusable style objects, justified alignment, and a practical legal disclaimer. PDF/A-2a + PDF/UA-1.")]
public static class RichTextExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Rich Text Styles",
                Author   = "BonniePDF",
                Subject  = "Inline mixed-style text using RichText spans. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, rich text, inline styles, bold, italic, underline, highlights, pdf/ua, pdf/a, BonniePDF",
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
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Rich Text Styles")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            Color     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "RichText lets you mix bold, italic, coloured, highlighted, and differently-sized spans within a single " +
                     "flowing paragraph. All formatting is per-span while the text wraps and aligns as a unit. " +
                     "Every RichText is automatically structured as a paragraph for screen readers.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── 1. Weight & Style ──────────────────────────────────────────────────

        Section(doc, "Weight & Style", "s-weight");

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 10) }
            .Span("Inline text can be ")
            .Span("bold", new TextStyle().Bold())
            .Span(", ")
            .Span("italic", new TextStyle().WithItalic())
            .Span(", or ")
            .Span("bold italic", new TextStyle().Bold().WithItalic())
            .Span(" — all within the same flowing paragraph. Scientific names like ")
            .Span("Homo sapiens", new TextStyle().WithItalic())
            .Span(" are a classic use of italic, while ")
            .Span("key terms", new TextStyle().Bold())
            .Span(" benefit from bold emphasis.")
        );

        // ── 2. Coloured Spans ──────────────────────────────────────────────────

        Section(doc, "Coloured Spans", "s-colors");

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 10) }
            .Span("Text can be ")
            .Span("red",       new TextStyle().WithColor(Color.Red))
            .Span(", ")
            .Span("blue",      new TextStyle().WithColor(ColorPalette.Blue[600]))
            .Span(", ")
            .Span("green",     new TextStyle().WithColor(ColorPalette.Green[600]))
            .Span(", or ")
            .Span("any colour", new TextStyle().WithColor(ColorPalette.Purple[600]))
            .Span(" you choose. Colour combines freely with weight: ")
            .Span("bold indigo", new TextStyle().Bold().WithColor(ColorPalette.Indigo[600]))
            .Span(" is a common choice for strong inline emphasis.")
        );

        // ── 3. Inline Decorations ──────────────────────────────────────────────

        Section(doc, "Inline Decorations", "s-decorations");

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 10) }
            .Span("Decorations are per-span: ")
            .Span("underlined words",     new TextStyle().Underline())
            .Span(", ")
            .Span("strikethrough text",   new TextStyle().Strikethrough())
            .Span(", and combinations like ")
            .Span("bold underlined",      new TextStyle().Bold().Underline())
            .Span(" or ")
            .Span("italic strikethrough", new TextStyle().WithItalic().Strikethrough())
            .Span(" all flow naturally within a single paragraph.")
        );

        // ── 4. Background Highlights ───────────────────────────────────────────

        Section(doc, "Background Highlights", "s-highlights");

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 10) }
            .Span("Background colours create a ")
            .Span("highlighter pen effect", new TextStyle().WithBackground(Color.FromHex("#fef08a")))
            .Span(" across any span. Combine with colour for ")
            .Span("bold red alert text", new TextStyle().Bold().WithColor(Color.FromHex("#991b1b")).WithBackground(Color.FromHex("#fecaca")))
            .Span(" or a ")
            .Span("green success note", new TextStyle().WithColor(Color.FromHex("#166534")).WithBackground(Color.FromHex("#bbf7d0")))
            .Span(" without breaking the paragraph flow.")
        );

        // ── 5. Mixed Font Sizes ────────────────────────────────────────────────

        Section(doc, "Mixed Font Sizes", "s-sizes");

        doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 10) }
            .Span("The same paragraph can carry ")
            .Span("larger text", new TextStyle().WithSize(18))
            .Span(" alongside ")
            .Span("smaller text", new TextStyle().WithSize(8))
            .Span(" — useful for superscript markers, footnote labels, or legal fine print. ")
            .Span("Line height adapts automatically to the tallest span on each line.")
        );

        // ── 6. Justified Alignment ─────────────────────────────────────────────

        Section(doc, "Justified Alignment", "s-justified");

        doc.Add(new RichText
        {
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 10),
        }
        .Span("Rich text supports ")
        .Span("full justification", new TextStyle().Bold())
        .Span(" — text is evenly distributed across the line width even when spans carry ")
        .Span("mixed styles", new TextStyle().WithItalic())
        .Span(". Each span maintains its own formatting while word-spacing is adjusted to fill the available width. " +
              "The final line remains left-aligned per typographic convention.")
        );

        // ── 7. Reusable Styles ─────────────────────────────────────────────────

        Section(doc, "Reusable Styles", "s-reusable");

        TextStyle labelStyle = new() { FontFamily = StandardFonts.Helvetica, FontWeight = FontWeight.Bold, Color = ColorPalette.Slate[800]  };
        TextStyle valueStyle = new() { FontFamily = StandardFonts.Helvetica,                               Color = ColorPalette.Indigo[600] };
        TextStyle unitStyle  = new() { FontFamily = StandardFonts.Helvetica, FontSize   = 9,               Color = ColorPalette.Slate[400]  };

        (string Label, string Value, string Unit)[] rows =
        [
            ("Temperature:", "23.5",    " °C"),
            ("Pressure:",    "1013.25", " hPa"),
            ("Humidity:",    "65",      " %"),
        ];

        foreach (var row in rows)
        {
            doc.Add(new RichText { Margin = new Thickness(0, 0, 0, 4) }
                .Span(row.Label, labelStyle)
                .Span(row.Value, valueStyle)
                .Span(row.Unit,  unitStyle));
        }

        // ── Practical Example ──────────────────────────────────────────────────

        doc.Add(new Line
        {
            Color     = ColorPalette.Slate[300],
            Thickness = 1,
            Margin    = new Thickness(0, 12, 0, 0),
        });

        Section(doc, "Practical Example: Legal Disclaimer", "s-legal");

        TextStyle disclaimerBase = new() { FontFamily = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Slate[600] };

        doc.Add(new RichText
        {
            DefaultStyle = disclaimerBase,
            Alignment    = TextAlignment.Justify,
            Margin       = new Thickness(0, 0, 0, 10),
        }
        .Span("IMPORTANT: ", new TextStyle().Bold().WithColor(Color.Red))
        .Span("By using this software, you agree to the ")
        .Span("Terms of Service", new TextStyle().Bold().Underline())
        .Span(" and ")
        .Span("Privacy Policy", new TextStyle().Bold().Underline())
        .Span(". All data is processed in accordance with ")
        .Span("GDPR", new TextStyle().Bold())
        .Span(" regulations. For questions, contact ")
        .Span("support@example.com", new TextStyle { FontFamily = StandardFonts.Helvetica, FontSize = 10, Color = ColorPalette.Blue[600] })
        .Span(".")
        );

        return doc;
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 13,
            Color     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 14, 0, 6),
        });
    }
}
