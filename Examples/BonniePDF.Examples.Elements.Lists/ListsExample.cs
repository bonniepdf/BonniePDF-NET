using BonniePDF;

namespace BonniePDF.Examples.Elements.Lists;

/// <summary>
/// Ordered and unordered lists — numbering formats, bullet styles, custom start
/// numbers, coloured bullets, and nested sub-lists. PDF/A-2a + PDF/UA-1: every
/// item is automatically structured as a real list for screen readers.
/// </summary>
[System.ComponentModel.DisplayName("Lists")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description(
    "Ordered and unordered lists — numbering formats, bullet styles, custom start numbers, coloured bullets, and nested sub-lists. PDF/A-2a + PDF/UA-1.")]
public static class ListsExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Lists",
                Author = "BonniePDF",
                Subject =
                    "Ordered and unordered lists — numbering formats, bullet styles, nesting, and coloured bullets. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, lists, ordered, unordered, nested",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Ordered lists ──────────────────────────────────────────────────────

        Category(doc, "Ordered Lists", "ordered");

        doc.Add(new TextBlock
        {
            Text = "Ordered lists number each item. The marker and the item text are held " +
                   "separately, so a screen reader can announce where an item sits in the list.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        VerticalStack s1 = Section("Decimal (1, 2, 3…)", "s-decimal");
        var decimalList = new OrderedList { NumberFormat = NumberFormat.Decimal, FontSize = 12, ItemSpacing = 6 };
        decimalList.AddItem("Plan the document structure before writing.");
        decimalList.AddItem("Apply semantic headings so assistive technology can navigate by section.");
        decimalList.AddItem("Tag every image with alternative text or mark it as decorative.");
        decimalList.AddItem("Validate the output with veraPDF before publishing.");
        s1.Add(decimalList);
        doc.Add(s1);

        VerticalStack s2 = Section("Upper Roman (I, II, III…)", "s-roman");
        var romanList = new OrderedList { NumberFormat = NumberFormat.UpperRoman, FontSize = 12, ItemSpacing = 6 };
        romanList.AddItem("Accessibility requirements");
        romanList.AddItem("Design guidelines");
        romanList.AddItem("Testing and validation");
        romanList.AddItem("Release checklist");
        s2.Add(romanList);
        doc.Add(s2);

        VerticalStack s3 = Section("Lowercase letters (a, b, c…)", "s-alpha");
        var alphaList = new OrderedList { NumberFormat = NumberFormat.LowerAlpha, FontSize = 12, ItemSpacing = 6 };
        alphaList.AddItem("Provide document language metadata.");
        alphaList.AddItem("Embed all fonts used in the file.");
        alphaList.AddItem("Include a colour profile so colours reproduce reliably.");
        s3.Add(alphaList);
        doc.Add(s3);

        VerticalStack s4 = Section("Custom start number", "s-start");
        s4.Add(new TextBlock
        {
            Text = "Numbering can begin at any value — useful when a list continues across sections.",
            Margin = new Thickness(0, 0, 0, 6),
        });
        var customStartList = new OrderedList
            { NumberFormat = NumberFormat.Decimal, StartNumber = 5, FontSize = 12, ItemSpacing = 6 };
        customStartList.AddItem("Review colour contrast ratios.");
        customStartList.AddItem("Confirm the reading order is logical.");
        customStartList.AddItem("Export and archive the final file.");
        s4.Add(customStartList);
        doc.Add(s4);

        VerticalStack s5 = Section("Nested ordered lists", "s-nested-ordered");
        s5.Add(new TextBlock
        {
            Text = "A sub-list nests under its parent item and can use a different numbering format.",
            Margin = new Thickness(0, 0, 0, 6),
        });
        var mainOrdered = new OrderedList { NumberFormat = NumberFormat.Decimal, FontSize = 12, ItemSpacing = 6 };
        mainOrdered.AddItem("Gather the source material.");
        mainOrdered.AddItem("Structure the document:");

        var nestedAlpha = new OrderedList
            { NumberFormat = NumberFormat.LowerAlpha, FontSize = 12, ItemSpacing = 4, Indent = 20 };
        nestedAlpha.AddItem("Outline the sections.");
        nestedAlpha.AddItem("Assign heading levels.");
        nestedAlpha.AddItem("Mark up lists and tables.");
        mainOrdered.AddNestedList(nestedAlpha);

        mainOrdered.AddItem("Review and validate.");
        mainOrdered.AddItem("Publish the accessible PDF.");
        s5.Add(mainOrdered);

        // ── Unordered lists ────────────────────────────────────────────────────
        doc.Add(s5);

        Category(doc, "Unordered Lists", "unordered");

        doc.Add(new TextBlock
        {
            Text =
                "Bullet items are structured the same way; only the marker changes. Several bullet styles are available.",
            Margin = new Thickness(0, 0, 0, 8),
        });

        VerticalStack s6 = Section("Disc (•)", "s-disc");
        var discList = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 6 };
        discList.AddItem("Screen readers announce list items and their position within the list.");
        discList.AddItem("Users can jump between lists using standard navigation shortcuts.");
        discList.AddItem("Disc is the default first-level bullet.");
        s6.Add(discList);
        doc.Add(s6);

        VerticalStack s7 = Section("Circle (◦)", "s-circle");
        var circleList = new UnorderedList { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 6 };
        circleList.AddItem("Circle bullets suit second-level points.");
        circleList.AddItem("They read as hollow discs.");
        circleList.AddItem("Pair them with disc bullets to signal hierarchy.");
        s7.Add(circleList);
        doc.Add(s7);

        VerticalStack s8 = Section("Square (■)", "s-square");
        var squareList = new UnorderedList { BulletStyle = BulletStyle.Square, FontSize = 12, ItemSpacing = 6 };
        squareList.AddItem("Square bullets draw attention to key notes.");
        squareList.AddItem("Useful for warnings or highlights.");
        squareList.AddItem("The marker takes any palette colour.");
        s8.Add(squareList);
        doc.Add(s8);

        VerticalStack s9 = Section("Dash (-)", "s-dash");
        var dashList = new UnorderedList { BulletStyle = BulletStyle.Dash, FontSize = 12, ItemSpacing = 6 };
        dashList.AddItem("Dash bullets give a plain, minimal look.");
        dashList.AddItem("Common in technical and code documentation.");
        dashList.AddItem("No special glyph is required.");
        s9.Add(dashList);
        doc.Add(s9);

        VerticalStack s10 = Section("Custom (*)", "s-custom");
        var customList = new UnorderedList
            { BulletStyle = BulletStyle.Custom, CustomBullet = "*", FontSize = 12, ItemSpacing = 6 };
        customList.AddItem("Any character can serve as a bullet.");
        customList.AddItem("Choose a marker supported by the selected font.");
        customList.AddItem("Choose one that suits the document tone.");
        s10.Add(customList);
        doc.Add(s10);

        VerticalStack s11 = Section("Coloured bullets", "s-coloured");
        var colouredList = new UnorderedList
        {
            BulletStyle = BulletStyle.Square,
            BulletColor = Color.Red,
            Color = ColorPalette.Slate[700],
            FontSize = 12,
            ItemSpacing = 6,
        };
        colouredList.AddItem("The bullet colour is independent of the text colour.");
        colouredList.AddItem("Handy for status markers or categorisation.");
        colouredList.AddItem("Any palette token or Color works.");
        s11.Add(colouredList);
        doc.Add(s11);

        VerticalStack s12 = Section("Nested unordered lists", "s-nested-unordered");
        var mainUnordered = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 6 };
        mainUnordered.AddItem("Compliance standards we target:");

        var nestedCircle = new UnorderedList
            { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 4, Indent = 20 };
        nestedCircle.AddItem("PDF/UA-1 (ISO 14289-1)");
        nestedCircle.AddItem("PDF/A-2a (ISO 19005-2)");
        nestedCircle.AddItem("WCAG 2.1 Level AA");
        mainUnordered.AddNestedList(nestedCircle);

        mainUnordered.AddItem("Each is validated in CI.");
        mainUnordered.AddItem("A failure blocks the release.");
        s12.Add(mainUnordered);
        doc.Add(s12);

        return doc;
    }

    private static void Category(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });
    }

    /// <summary>
    /// A keep-together block that opens with a section heading. Everything added to it moves to the
    /// next page as one piece, so a heading never sits at the foot of a page apart from its list,
    /// and a nested list is never parted from the item it belongs to.
    /// </summary>
    private static VerticalStack Section(string title, string tocId)
    {
        VerticalStack stack = new() { BreakAllowed = false };
        stack.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 14, 0, 6),
        });
        return stack;
    }
}
