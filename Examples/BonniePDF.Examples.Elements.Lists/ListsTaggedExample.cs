using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Lists;

/// <summary>
/// PDF/UA-1 tagged list example.
///
/// Demonstrates the required <c>L &gt; LI &gt; Lbl + LBody</c> structure tree for both
/// <see cref="OrderedList"/> and <see cref="UnorderedList"/>, including a nested sub-list.
///
/// Mirror of the TypeScript examples/src/elements/lists-tagged.ts.
/// Acceptance: VeraPDF profile ua1 reports zero failures (tools/verapdf/validate.sh).
/// </summary>
[System.ComponentModel.DisplayName("Lists (PDF/UA-1)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("PDF/UA-1 tagged ordered and unordered lists - L > LI > Lbl + LBody structure for full screen-reader compatibility.")]
public static class ListsTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Accessible Lists - Tagged (PDF/UA-1)",
                Author = "BonniePDF",
                Subject = "PDF/UA-1 tagged ordered and unordered lists with nested items.",
                Keywords = "pdf, pdf/ua, accessibility, lists, ordered, unordered",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Ordered list ──────────────────────────────────────────────────────

        doc.Add(new Heading("Ordered Lists")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text = "Each numbered item maps to an LI structure element containing a Lbl (the number) and an LBody (the text).",
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new Heading("Decimal numbered")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 14, 0, 6),
        });

        var decimalList = new OrderedList { NumberFormat = NumberFormat.Decimal, FontSize = 12, ItemSpacing = 6 };
        decimalList.AddItem("Plan the document structure before writing.");
        decimalList.AddItem("Apply semantic headings so assistive technology can navigate by section.");
        decimalList.AddItem("Tag every image with alternative text or mark it as an artifact.");
        decimalList.AddItem("Validate the output with VeraPDF before publishing.");
        doc.Add(decimalList);

        doc.Add(new Heading("Roman numerals with nested sub-list")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 14, 0, 6),
        });

        var romanList = new OrderedList { NumberFormat = NumberFormat.UpperRoman, FontSize = 12, ItemSpacing = 6 };
        romanList.AddItem("Accessibility requirements");

        var nestedAlpha = new OrderedList
        {
            NumberFormat = NumberFormat.LowerAlpha,
            FontSize = 12,
            ItemSpacing = 4,
            Indent = 20,
        };
        nestedAlpha.AddItem("Provide document language metadata.");
        nestedAlpha.AddItem("Embed all fonts used in the file.");
        nestedAlpha.AddItem("Include an OutputIntent colour profile.");
        romanList.AddNestedList(nestedAlpha);

        romanList.AddItem("Design guidelines");
        romanList.AddItem("Testing and validation");
        doc.Add(romanList);

        // ── Unordered list ────────────────────────────────────────────────────

        doc.Add(new Heading("Unordered Lists")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text = "Bullet items follow the same L > LI > Lbl + LBody tree structure as numbered lists.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new Heading("Disc bullets")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 14, 0, 6),
        });

        var discList = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 6 };
        discList.AddItem("Screen readers announce list items and their position within the list.");
        discList.AddItem("Users can jump between lists using standard navigation shortcuts.");
        discList.AddItem("Nested lists are announced with their own list depth.");
        doc.Add(discList);

        doc.Add(new Heading("Square bullets with nested sub-list")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 14, 0, 6),
        });

        var squareList = new UnorderedList { BulletStyle = BulletStyle.Square, FontSize = 12, ItemSpacing = 6 };
        squareList.AddItem("PDF/UA-1 (ISO 14289-1)");

        var nestedCircle = new UnorderedList
        {
            BulletStyle = BulletStyle.Circle,
            FontSize = 12,
            ItemSpacing = 4,
            Indent = 20,
        };
        nestedCircle.AddItem("Requires tagging to be enabled.");
        nestedCircle.AddItem("Requires a document language declaration.");
        nestedCircle.AddItem("Requires all content to be either tagged or marked as an artifact.");
        squareList.AddNestedList(nestedCircle);

        squareList.AddItem("PDF/A-1b (ISO 19005-1)");
        squareList.AddItem("WCAG 2.1 Level AA");
        doc.Add(squareList);

        return doc;
    }
}
