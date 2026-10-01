using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.CrossReferences;

/// <summary>
/// Cross-references: <see cref="Section"/> (named anchors),
/// <see cref="SectionLink"/> (clickable internal jumps), and
/// <see cref="PageNumberOf"/> ("see page 42"). The contents block sits on page 1,
/// ahead of the chapters it points at, so every reference is resolved by the
/// two-pass layout before the page is drawn.
/// </summary>
[System.ComponentModel.DisplayName("Cross-References")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Section anchors, internal links, and page references.")]
public static class CrossReferencesExample
{
    private static readonly (string Id, string Title, string Body)[] Chapters =
    {
        ("ch-anchors", "Anchors",
            "A Section marks an invisible, zero-height position in the flow. Point links and page references at it by id."),
        ("ch-links", "Internal links",
            "A SectionLink resolves its target page after layout and emits a clickable GoTo annotation - no manual page numbers."),
        ("ch-pagerefs", "Page references",
            "A PageNumberOf prints the page a named anchor landed on, so \"see page 4\" stays correct as content reflows."),
    };

    public static Document Generate()
    {
        Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[700]);
        doc.Settings.DisplayDocTitle = true;

        doc.Metadata.Title = "Cross-Reference Field Guide";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Section anchors, internal links, and page references";

        // Anchor for the "back to contents" references further down.
        doc.Add(new Section("contents"));

        doc.Add(new Heading
        {
            Text = "Cross-Reference Field Guide",
            TocIdentifier = "title",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "This guide is a live demonstration of BonniePDF cross-references. The contents " +
                   "below link to chapters that appear on later pages, and each shows the resolved " +
                   "page number - all wired up automatically.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new Heading
        {
            Text = "Contents",
            TocIdentifier = "contents-heading",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 8),
        });

        foreach ((string id, string title, _) in Chapters)
        {
            // Clickable jump - a standalone Link block (mirrors the Hyperlinks recipe).
            doc.Add(new SectionLink($"> {title}", id)
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 13,
                Color = ColorPalette.Blue[600],
                Margin = new Thickness(0, 0, 0, 2),
            });

            // "begins on page N" - a label + a live PageNumberOf on one row.
            var label = new TextBlock { Text = "begins on page", FontSize = 11, Color = ColorPalette.Slate[500] };
            var pageRef = new PageNumberOf(id)
            {
                FontName = StandardFonts.HelveticaBold,
                FontSize = 11,
                Color = ColorPalette.Slate[700],
            };

            var row = new HorizontalStack { Spacing = 4, Margin = new Thickness(16, 0, 0, 10) };
            row.Add(label, label.GetPreferredWidth());
            row.Add(pageRef);
            doc.Add(row);
        }

        // One chapter per page so the page numbers are visibly distinct.
        foreach ((string id, string title, string body) in Chapters)
        {
            doc.Add(new PageBreak());

            // The anchor sits exactly where the chapter starts.
            doc.Add(new Section(id));

            doc.Add(new Heading
            {
                Text = title,
                TocIdentifier = $"heading-{id}",
                Level = 2,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 18,
                Color = ColorPalette.Slate[800],
                Margin = new Thickness(0, 0, 0, 8),
            });

            doc.Add(new TextBlock { Text = body, Margin = new Thickness(0, 0, 0, 16) });

            // Back-reference to the contents page, resolved the same way.
            doc.Add(new SectionLink("< Back to Contents", "contents")
            {
                FontSize = 12,
                Color = ColorPalette.Blue[600],
            });
        }

        return doc;
    }
}
