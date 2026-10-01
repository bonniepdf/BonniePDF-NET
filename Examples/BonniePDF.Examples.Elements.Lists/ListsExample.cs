using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Lists;

/// <summary>
/// Ordered and unordered lists — numbering formats, bullet styles, custom start
/// numbers, coloured bullets, and nested sub-lists. PDF/A-2a + PDF/UA-1: every
/// item is automatically structured as a real list for screen readers.
/// </summary>
[System.ComponentModel.DisplayName("Lists")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Ordered and unordered lists — numbering formats, bullet styles, custom start numbers, coloured bullets, and nested sub-lists. PDF/A-2a + PDF/UA-1.")]
public static class ListsExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Lists",
                Author   = "BonniePDF",
                Subject  = "Ordered and unordered lists — numbering formats, bullet styles, nesting, and coloured bullets. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, lists, ordered, unordered, nested",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Ordered lists ──────────────────────────────────────────────────────

        Category(doc, "Ordered Lists", "ordered");

        doc.Add(new TextBlock
        {
            Text   = "Ordered lists number each item. The marker and the item text are held " +
                     "separately, so a screen reader can announce where an item sits in the list.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        Section(doc, "Decimal (1, 2, 3…)", "s-decimal");
        var decimalList = new OrderedList { NumberFormat = NumberFormat.Decimal, FontSize = 12, ItemSpacing = 6 };
        decimalList.AddItem("Plan the document structure before writing.");
        decimalList.AddItem("Apply semantic headings so assistive technology can navigate by section.");
        decimalList.AddItem("Tag every image with alternative text or mark it as decorative.");
        decimalList.AddItem("Validate the output with veraPDF before publishing.");
        doc.Add(decimalList);

        Section(doc, "Upper Roman (I, II, III…)", "s-roman");
        var romanList = new OrderedList { NumberFormat = NumberFormat.UpperRoman, FontSize = 12, ItemSpacing = 6 };
        romanList.AddItem("Accessibility requirements");
        romanList.AddItem("Design guidelines");
        romanList.AddItem("Testing and validation");
        romanList.AddItem("Release checklist");
        doc.Add(romanList);

        Section(doc, "Lowercase letters (a, b, c…)", "s-alpha");
        var alphaList = new OrderedList { NumberFormat = NumberFormat.LowerAlpha, FontSize = 12, ItemSpacing = 6 };
        alphaList.AddItem("Provide document language metadata.");
        alphaList.AddItem("Embed all fonts used in the file.");
        alphaList.AddItem("Include a colour profile so colours reproduce reliably.");
        doc.Add(alphaList);

        Section(doc, "Custom start number", "s-start");
        doc.Add(new TextBlock
        {
            Text   = "Numbering can begin at any value — useful when a list continues across sections.",
            Margin = new Thickness(0, 0, 0, 6),
        });
        var customStartList = new OrderedList { NumberFormat = NumberFormat.Decimal, StartNumber = 5, FontSize = 12, ItemSpacing = 6 };
        customStartList.AddItem("Review colour contrast ratios.");
        customStartList.AddItem("Confirm the reading order is logical.");
        customStartList.AddItem("Export and archive the final file.");
        doc.Add(customStartList);

        Section(doc, "Nested ordered lists", "s-nested-ordered");
        doc.Add(new TextBlock
        {
            Text   = "A sub-list nests under its parent item and can use a different numbering format.",
            Margin = new Thickness(0, 0, 0, 6),
        });
        var mainOrdered = new OrderedList { NumberFormat = NumberFormat.Decimal, FontSize = 12, ItemSpacing = 6 };
        mainOrdered.AddItem("Gather the source material.");
        mainOrdered.AddItem("Structure the document:");

        var nestedAlpha = new OrderedList { NumberFormat = NumberFormat.LowerAlpha, FontSize = 12, ItemSpacing = 4, Indent = 20 };
        nestedAlpha.AddItem("Outline the sections.");
        nestedAlpha.AddItem("Assign heading levels.");
        nestedAlpha.AddItem("Mark up lists and tables.");
        mainOrdered.AddNestedList(nestedAlpha);

        mainOrdered.AddItem("Review and validate.");
        mainOrdered.AddItem("Publish the accessible PDF.");
        doc.Add(mainOrdered);

        // ── Unordered lists ────────────────────────────────────────────────────

        Category(doc, "Unordered Lists", "unordered");

        doc.Add(new TextBlock
        {
            Text   = "Bullet items are structured the same way; only the marker changes. Several bullet styles are available.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        Section(doc, "Disc (•)", "s-disc");
        var discList = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 6 };
        discList.AddItem("Screen readers announce list items and their position within the list.");
        discList.AddItem("Users can jump between lists using standard navigation shortcuts.");
        discList.AddItem("Disc is the default first-level bullet.");
        doc.Add(discList);

        Section(doc, "Circle (◦)", "s-circle");
        var circleList = new UnorderedList { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 6 };
        circleList.AddItem("Circle bullets suit second-level points.");
        circleList.AddItem("They read as hollow discs.");
        circleList.AddItem("Pair them with disc bullets to signal hierarchy.");
        doc.Add(circleList);

        Section(doc, "Square (■)", "s-square");
        var squareList = new UnorderedList { BulletStyle = BulletStyle.Square, FontSize = 12, ItemSpacing = 6 };
        squareList.AddItem("Square bullets draw attention to key notes.");
        squareList.AddItem("Useful for warnings or highlights.");
        squareList.AddItem("The marker takes any palette colour.");
        doc.Add(squareList);

        Section(doc, "Dash (-)", "s-dash");
        var dashList = new UnorderedList { BulletStyle = BulletStyle.Dash, FontSize = 12, ItemSpacing = 6 };
        dashList.AddItem("Dash bullets give a plain, minimal look.");
        dashList.AddItem("Common in technical and code documentation.");
        dashList.AddItem("No special glyph is required.");
        doc.Add(dashList);

        Section(doc, "Custom (★)", "s-custom");
        var customList = new UnorderedList { BulletStyle = BulletStyle.Custom, CustomBullet = "★", FontSize = 12, ItemSpacing = 6 };
        customList.AddItem("Any character can serve as a bullet.");
        customList.AddItem("Stars, arrows, or checkmarks all work.");
        customList.AddItem("Choose one that suits the document tone.");
        doc.Add(customList);

        Section(doc, "Coloured bullets", "s-coloured");
        var colouredList = new UnorderedList
        {
            BulletStyle = BulletStyle.Square,
            BulletColor = Color.Red,
            Color   = ColorPalette.Slate[700],
            FontSize    = 12,
            ItemSpacing = 6,
        };
        colouredList.AddItem("The bullet colour is independent of the text colour.");
        colouredList.AddItem("Handy for status markers or categorisation.");
        colouredList.AddItem("Any palette token or Color works.");
        doc.Add(colouredList);

        Section(doc, "Nested unordered lists", "s-nested-unordered");
        var mainUnordered = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 6 };
        mainUnordered.AddItem("Compliance standards we target:");

        var nestedCircle = new UnorderedList { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 4, Indent = 20 };
        nestedCircle.AddItem("PDF/UA-1 (ISO 14289-1)");
        nestedCircle.AddItem("PDF/A-2a (ISO 19005-2)");
        nestedCircle.AddItem("WCAG 2.1 Level AA");
        mainUnordered.AddNestedList(nestedCircle);

        mainUnordered.AddItem("Each is validated in CI.");
        mainUnordered.AddItem("A failure blocks the release.");
        doc.Add(mainUnordered);

        return doc;
    }

    private static void Category(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            Color     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 14,
            Color     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 14, 0, 6),
        });
    }
}
