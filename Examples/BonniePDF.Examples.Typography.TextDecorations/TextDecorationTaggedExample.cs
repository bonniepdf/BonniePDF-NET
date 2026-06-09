using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.TextDecorations;

/// <summary>
/// PDF/UA-1 tagged text-decorations example.
///
/// Demonstrates underline and strikethrough decorations. Section headers are /H2 structure
/// elements; decorated paragraphs are /P. Decorations (underline, strikethrough) are visual
/// properties and do not change the structure-tree role.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Text Decorations (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged text with underline and strikethrough decorations - all paragraphs tagged as /P.")]
public static class TextDecorationTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Text Decorations - Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates text decorations in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, text, underline, strikethrough",
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
                    .WithSize(12)
                    .WithColor(Color.Slate700),
            },
        };

        doc.Add(new Heading("Text Decorations - PDF/UA-1")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = Color.Slate800,
            Margin        = new Thickness(0, 0, 0, 14),
        });

        doc.Add(new TextBlock
        {
            Text   = "Underline and strikethrough are presentational properties that do not change the structure-tree type of a text block. All decorated paragraphs below are /P structure elements.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── Underline ──────────────────────────────────────────────────────────

        Section(doc, "Underline", "s-underline");

        doc.Add(new TextBlock
        {
            Text        = "This text has an underline decoration applied.",
            TextDecoration = TextDecoration.Underline,
            TextColor   = Color.Slate700,
            Margin      = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text        = "Underlines can mark hyperlinks, emphasis, or headings in informal documents.",
            TextDecoration = TextDecoration.Underline,
            TextColor   = Color.Slate700,
            Margin      = new Thickness(0, 0, 0, 8),
        });

        // ── Strikethrough ──────────────────────────────────────────────────────

        Section(doc, "Strikethrough", "s-strike");

        doc.Add(new TextBlock
        {
            Text          = "This text has a strikethrough decoration, useful for showing deleted or superseded content.",
            TextDecoration = TextDecoration.Strikethrough,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text          = "Strikethrough visually signals content that has been withdrawn or revised.",
            TextDecoration = TextDecoration.Strikethrough,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        // ── Combined decorations ───────────────────────────────────────────────

        Section(doc, "Combined Decorations", "s-combined");

        doc.Add(new TextBlock
        {
            Text           = "This text has both underline and strikethrough applied simultaneously.",
            TextDecoration = TextDecoration.Underline | TextDecoration.Strikethrough,
            TextColor     = Color.Slate700,
            Margin        = new Thickness(0, 0, 0, 8),
        });

        // ── Decoration thickness scales with font size ─────────────────────────

        Section(doc, "Decoration Thickness Scales with Font Size", "s-sizes");

        foreach (int size in new[] { 8, 12, 18, 24, 36 })
        {
            doc.Add(new TextBlock
            {
                Text           = $"Underline at {size}pt",
                FontSize       = size,
                TextDecoration = TextDecoration.Underline,
                TextColor = Color.Slate700,
                Margin    = new Thickness(0, 0, 0, 4),
            });
        }

        // ── Practical: price list ──────────────────────────────────────────────

        Section(doc, "Practical Example: Price List", "s-prices");

        doc.Add(new TextBlock
        {
            Text          = "Original Price: £199.99",
            FontSize      = 14,
            TextColor     = Color.Slate400,
            TextDecoration = TextDecoration.Strikethrough,
            Margin        = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text           = "Sale Price: £149.99",
            FontName       = StandardFonts.HelveticaBold,
            FontSize       = 18,
            TextColor      = Color.Red,
            TextDecoration = TextDecoration.Underline,
            Margin    = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text      = "You save £50.00!",
            FontSize  = 11,
            TextColor = Color.Green600,
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
            TextColor     = Color.Indigo600,
            Margin        = new Thickness(0, 12, 0, 6),
        });
    }
}
