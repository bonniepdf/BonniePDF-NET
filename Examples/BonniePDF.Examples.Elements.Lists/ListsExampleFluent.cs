using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Lists;

/// <summary>
/// Demonstrates ordered and unordered list elements using the Fluent API.
/// OrderedList and UnorderedList elements are inserted via the Element() escape hatch
/// since there are no dedicated fluent terminal methods for them.
/// The document spans four pages, each added via a separate doc.Page() call.
/// </summary>
[System.ComponentModel.DisplayName("Lists (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Ordered and unordered bullet point lists using the Fluent API.")]
public static class ListsExampleFluent
{
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title  = "List Examples (Fluent API)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.Version     = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12));

            // ── Page 1: Ordered lists ─────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Ordered List Examples")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    // Decimal
                    ListSectionHeader(col, "Decimal Numbered List (1, 2, 3...)");

                    var decimalList = new OrderedList { NumberFormat = NumberFormat.Decimal, StartNumber = 1, FontSize = 12, ItemSpacing = 8 };
                    decimalList.AddItem("First item in the list");
                    decimalList.AddItem("Second item with more text to demonstrate wrapping");
                    decimalList.AddItem("Third item");
                    decimalList.AddItem("Fourth item showing the decimal numbering format");
                    col.Item().Element(decimalList);

                    // Upper Roman
                    ListSectionHeader(col, "Upper Roman Numerals (I, II, III...)", topMargin: 16);

                    var romanList = new OrderedList { NumberFormat = NumberFormat.UpperRoman, StartNumber = 1, FontSize = 12, ItemSpacing = 8 };
                    romanList.AddItem("First section");
                    romanList.AddItem("Second section");
                    romanList.AddItem("Third section");
                    romanList.AddItem("Fourth section");
                    col.Item().Element(romanList);

                    // Lowercase alpha
                    ListSectionHeader(col, "Lowercase Letters (a, b, c...)", topMargin: 16);

                    var alphaList = new OrderedList { NumberFormat = NumberFormat.LowerAlpha, StartNumber = 1, FontSize = 12, ItemSpacing = 8 };
                    alphaList.AddItem("Sub-point one");
                    alphaList.AddItem("Sub-point two");
                    alphaList.AddItem("Sub-point three");
                    col.Item().Element(alphaList);

                    // Custom start number
                    ListSectionHeader(col, "List Starting at Number 5", topMargin: 16);

                    var customStartList = new OrderedList { NumberFormat = NumberFormat.Decimal, StartNumber = 5, FontSize = 12, ItemSpacing = 8 };
                    customStartList.AddItem("Item number 5");
                    customStartList.AddItem("Item number 6");
                    customStartList.AddItem("Item number 7");
                    col.Item().Element(customStartList);
                });
            });

            // ── Page 2: Nested ordered lists ──────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Nested Ordered Lists")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    var nestedOrdered = new OrderedList { NumberFormat = NumberFormat.LowerAlpha, StartNumber = 1, FontSize = 12, ItemSpacing = 6, Indent = 20 };
                    nestedOrdered.AddItem("Nested item A");
                    nestedOrdered.AddItem("Nested item B");
                    nestedOrdered.AddItem("Nested item C");

                    var mainOrdered = new OrderedList { NumberFormat = NumberFormat.Decimal, StartNumber = 1, FontSize = 12, ItemSpacing = 8 };
                    mainOrdered.AddItem("Main item one");
                    mainOrdered.AddItem("Main item two with nested list below");
                    mainOrdered.AddNestedList(nestedOrdered);
                    mainOrdered.AddItem("Main item three");
                    mainOrdered.AddItem("Main item four");

                    col.Item().Element(mainOrdered);
                });
            });

            // ── Page 3: Unordered lists ───────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Unordered List Examples")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    // Disc
                    ListSectionHeader(col, "Disc Bullet Style (•)");
                    var discList = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 8 };
                    discList.AddItem("First feature point");
                    discList.AddItem("Second feature point with longer text to show wrapping");
                    discList.AddItem("Third feature point");
                    discList.AddItem("Fourth feature point");
                    col.Item().Element(discList);

                    // Circle
                    ListSectionHeader(col, "Circle Bullet Style (◦)", topMargin: 16);
                    var circleList = new UnorderedList { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 8 };
                    circleList.AddItem("Sub-feature one");
                    circleList.AddItem("Sub-feature two");
                    circleList.AddItem("Sub-feature three");
                    col.Item().Element(circleList);

                    // Square
                    ListSectionHeader(col, "Square Bullet Style (■)", topMargin: 16);
                    var squareList = new UnorderedList { BulletStyle = BulletStyle.Square, FontSize = 12, ItemSpacing = 8 };
                    squareList.AddItem("Important note one");
                    squareList.AddItem("Important note two");
                    squareList.AddItem("Important note three");
                    col.Item().Element(squareList);

                    // Dash
                    ListSectionHeader(col, "Dash Bullet Style (-)", topMargin: 16);
                    var dashList = new UnorderedList { BulletStyle = BulletStyle.Dash, FontSize = 12, ItemSpacing = 8 };
                    dashList.AddItem("Quick point one");
                    dashList.AddItem("Quick point two");
                    dashList.AddItem("Quick point three");
                    col.Item().Element(dashList);

                    // Custom star
                    ListSectionHeader(col, "Custom Bullet Style (★)", topMargin: 16);
                    var customList = new UnorderedList { BulletStyle = BulletStyle.Custom, CustomBullet = "★", FontSize = 12, ItemSpacing = 8 };
                    customList.AddItem("Starred item one");
                    customList.AddItem("Starred item two");
                    customList.AddItem("Starred item three");
                    col.Item().Element(customList);
                });
            });

            // ── Page 4: Nested unordered + coloured bullets ───────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("Nested Unordered Lists")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    var nestedUnordered = new UnorderedList { BulletStyle = BulletStyle.Circle, FontSize = 12, ItemSpacing = 6, Indent = 20 };
                    nestedUnordered.AddItem("Sub-item one");
                    nestedUnordered.AddItem("Sub-item two");
                    nestedUnordered.AddItem("Sub-item three");

                    var mainUnordered = new UnorderedList { BulletStyle = BulletStyle.Disc, FontSize = 12, ItemSpacing = 8 };
                    mainUnordered.AddItem("Main category one");
                    mainUnordered.AddItem("Main category two with sub-items");
                    
                    mainUnordered.AddNestedList(nestedUnordered);

                    mainUnordered.AddItem("Main category three");
                    mainUnordered.AddItem("Main category four");
                    col.Item().Element(mainUnordered);

                    // Coloured bullets
                    ListSectionHeader(col, "Colored Bullets", topMargin: 16);
                    var coloredList = new UnorderedList
                    {
                        BulletStyle = BulletStyle.Square,
                        BulletColor = Color.Red,
                        TextColor   = Color.Slate600,
                        FontSize    = 12,
                        ItemSpacing = 8
                    };

                    coloredList.AddItem("Red bullet point");
                    coloredList.AddItem("Another red bullet point");
                    coloredList.AddItem("Third red bullet point");

                    col.Item().Element(coloredList);
                });
            });

        }).Build();

    }

    /// <summary>Adds a bold Slate700 section header, with optional top margin.</summary>
    private static void ListSectionHeader(Fluent.Descriptors.ColumnDescriptor col, string title, float topMargin = 0)
    {
        col.Item()
            .Text(title)
            .Font(StandardFonts.HelveticaBold)
            .FontSize(14)
            .TextColor(Color.Slate700)
            .Margin(0, topMargin, 0, 0);
    }
}

