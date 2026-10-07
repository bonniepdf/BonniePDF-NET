using BonniePDF;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// Automatic pagination for long-form text in an accessible, archival document
/// (PDF/A-2a + PDF/UA-1). Three self-documenting sections describe what the
/// reader is seeing; the body is long enough to flow across several pages.
/// Headings stay semantic (H1, H2), paragraphs stay P, and the running
/// "Page N of M" footer is marked as decorative.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("TextBlock Overflow")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description(
    "Long-form text flowing across multiple pages - self-documenting sections stay tagged as they paginate, with a decorative page-number footer.")]
public static class TextBlockOverflowExample
{
    private static readonly (string Title, string Intro)[] Sections =
    {
        ("Continuous Text Flow",
            "The paragraphs in this section are ordinary TextBlock elements added one after another. When a page runs out of vertical space, BonniePDF breaks to a new page and continues the flow automatically - you never position text or insert page breaks by hand."),

        ("Structure Survives the Page Break",
            "Automatic pagination does not flatten the document. Each section heading stays semantically structured, so a screen reader can navigate heading-to-heading even though the text spans several pages."),

        ("The Footer is Decorative",
            "The \"Page N of M\" line at the foot of every page comes from a footer template. It is marked as decorative, so assistive technology skips it; the total page count is resolved after layout, once the number of pages is known."),
    };

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "TextBlock Overflow Demo",
                Author = "BonniePDF",
                Subject =
                    "Demonstrates long-form text flowing across page boundaries in an accessible PDF/A-2a + PDF/UA-1 document.",
                Keywords = "pdf, accessibility, text, overflow, pagination, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),

                // Page numbers in the footer are decorative - screen readers skip them.
                FooterTemplate = pageNum => new TextBlock
                {
                    Text = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 9,
                    Color = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        doc.Add(new Heading("Long-Form Text & Automatic Pagination")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text =
                "This document shows how BonniePDF flows long-form text across page boundaries while keeping it fully tagged. The three sections below describe what to look for; the body text is intentionally long enough to span several pages.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        var source = Placeholders.WithSeed(502);

        for (int i = 0; i < Sections.Length; i++)
        {
            (string title, string intro) = Sections[i];

            doc.Add(new Heading(title)
            {
                TocIdentifier = $"section-{i + 1}",
                Level = 2,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                Color = ColorPalette.Indigo[600],
                Margin = new Thickness(0, 16, 0, 6),
            });

            doc.Add(new TextBlock
            {
                Text = intro,
                Margin = new Thickness(0, 0, 0, 8),
            });

            // Filler paragraphs to give the document enough length to paginate.
            for (int p = 0; p < 3; p++)
            {
                doc.Add(new TextBlock
                {
                    Text = source.Paragraph(),
                    Margin = new Thickness(0, 0, 0, 8),
                });
            }
        }

        return doc;
    }
}
