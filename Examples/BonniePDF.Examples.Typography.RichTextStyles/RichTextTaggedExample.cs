using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.RichTextStyles;

/// <summary>
/// PDF/UA-1 tagged rich-text example.
///
/// Mixed-style inline text is carried by <see cref="RichText"/> elements. Each section is
/// introduced by a semantic <see cref="Heading"/>, and descriptive paragraphs are
/// <see cref="TextBlock"/> elements. VeraPDF sees /H2 for section headers and /P for every
/// RichText and TextBlock.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Rich Text Styles (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged mixed-style rich text — section headings tagged as /H2, rich paragraphs as /P.")]
public static class RichTextTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Rich Text (Mixed Styling) — Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates mixed inline text styles in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, rich text, inline styles",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 60,
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(Color.Slate700),
            },
        };

        doc.Add(new Heading("Rich Text (Mixed Styling) — PDF/UA-1")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 14),
        });

        // ── Bold words ─────────────────────────────────────────────────────────

        Section(doc, "Bold Words in a Paragraph", "h-bold");

        doc.Add(new RichText()
            .Span("This paragraph contains ")
            .Span("bold text", new TextStyle().Bold())
            .Span(" mixed with regular text. The ")
            .Span("bold words", new TextStyle().Bold())
            .Span(" stand out while the text flows naturally as a single paragraph.")
        );

        // ── Italic text ────────────────────────────────────────────────────────

        Section(doc, "Italic Text", "h-italic");

        doc.Add(new RichText()
            .Span("Scientific names are written in italic, such as ")
            .Span("Homo sapiens", new TextStyle().WithItalic())
            .Span(" or ")
            .Span("Escherichia coli", new TextStyle().WithItalic())
            .Span(". This convention distinguishes taxonomic nomenclature from surrounding text.")
        );

        // ── Bold + Italic combined ─────────────────────────────────────────────

        Section(doc, "Bold and Italic Combined", "h-bold-italic");

        doc.Add(new RichText()
            .Span("You can combine ")
            .Span("bold", new TextStyle().Bold())
            .Span(", ")
            .Span("italic", new TextStyle().WithItalic())
            .Span(", and ")
            .Span("bold italic", new TextStyle().Bold().WithItalic())
            .Span(" styles within a single flowing paragraph.")
        );

        // ── Coloured spans ─────────────────────────────────────────────────────

        Section(doc, "Colored Spans", "h-colors");

        doc.Add(new RichText()
            .Span("Text can be ")
            .Span("red", new TextStyle().WithColor(Color.Red))
            .Span(", ")
            .Span("blue", new TextStyle().WithColor(Color.Blue600))
            .Span(", ")
            .Span("green", new TextStyle().WithColor(Color.Green600))
            .Span(", or ")
            .Span("any color", new TextStyle().WithColor(Color.Purple600))
            .Span(" you choose. Colors can be combined with ")
            .Span("bold colored text", new TextStyle().Bold().WithColor(Color.Indigo600))
            .Span(" for maximum emphasis.")
        );

        // ── Inline decorations ─────────────────────────────────────────────────

        Section(doc, "Inline Decorations", "h-decorations");

        doc.Add(new RichText()
            .Span("Decorations work per-span: ")
            .Span("underlined words", new TextStyle().Underline())
            .Span(" and ")
            .Span("strikethrough words", new TextStyle().Strikethrough())
            .Span(" can appear anywhere in the paragraph, including ")
            .Span("bold underlined", new TextStyle().Bold().Underline())
            .Span(" combinations.")
        );

        // ── Justified alignment ────────────────────────────────────────────────

        Section(doc, "Justified Alignment", "h-justified");

        doc.Add(new RichText
        {
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 10),
        }
        .Span("Rich text supports ")
        .Span("full justification", new TextStyle().Bold())
        .Span(" where the text is evenly distributed across the line width. This works with ")
        .Span("mixed styles", new TextStyle().WithItalic())
        .Span(" — each span maintains its own formatting while the spacing between words is adjusted to fill the available width.")
        );

        // ── Practical: legal disclaimer ────────────────────────────────────────

        Section(doc, "Practical Example: Legal Disclaimer", "h-legal");

        TextStyle disclaimerBase  = new() { FontFamily = StandardFonts.Helvetica, FontSize = 10, Color = Color.Slate600 };
        TextStyle disclaimerBold  = new TextStyle().Bold().WithColor(Color.Red);
        TextStyle disclaimerLink  = new() { FontFamily = StandardFonts.Helvetica, FontSize = 10 };

        doc.Add(new RichText
        {
            DefaultStyle = disclaimerBase,
            Alignment    = TextAlignment.Justify,
        }
        .Span("IMPORTANT: ", disclaimerBold)
        .Span("By using this software, you agree to the ")
        .Span("Terms of Service", new TextStyle().Bold().Underline())
        .Span(" and ")
        .Span("Privacy Policy", new TextStyle().Bold().Underline())
        .Span(". All data is processed in accordance with ")
        .Span("GDPR", new TextStyle().Bold())
        .Span(" regulations. For questions, contact ")
        .Span("support@example.com", new TextStyle { FontFamily = StandardFonts.Helvetica, FontSize = 10, Color = Color.Blue600 })
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
            TextColor     = Color.Indigo600,
            Margin        = new Thickness(0, 14, 0, 6),
        });
    }
}
