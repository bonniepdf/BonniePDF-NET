using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._04_Elements;

/// <summary>
/// Demonstrates the List elements (Ordered and Unordered)
/// </summary>
public class ListExample : ExampleBase
{
    public override string Id => "Lists";
    public override string Title => "Lists";
    public override string Description => "Ordered and unordered bullet point lists.";
    public override string FileName => "list_example.pdf";
    public override ExampleCategory Category => ExampleCategory.Elements;
    
    public override string Generate(string outputPath)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set document metadata
        doc.Metadata.Title = "List Examples";
        doc.Metadata.Author = "BonniePDF";

        // ==========================================
        // ORDERED LISTS
        // ==========================================

        // Title
        doc.Add(new TextBlock
        {
            Text = "Ordered List Examples",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Decimal numbered list
        doc.Add(new TextBlock
        {
            Text = "Decimal Numbered List (1, 2, 3...)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700
        });

        OrderedList decimalList = new()
        {
            NumberFormat = NumberFormat.Decimal,
            StartNumber = 1,
            FontSize = 12,
            ItemSpacing = 8
        };
        decimalList.AddItem("First item in the list");
        decimalList.AddItem("Second item with more text to demonstrate wrapping");
        decimalList.AddItem("Third item");
        decimalList.AddItem("Fourth item showing the decimal numbering format");

        doc.Add(decimalList);

        // Upper Roman numerals
        doc.Add(new TextBlock
        {
            Text = "Upper Roman Numerals (I, II, III...)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        OrderedList romanList = new()
        {
            NumberFormat = NumberFormat.UpperRoman,
            StartNumber = 1,
            FontSize = 12,
            ItemSpacing = 8
        };
        romanList.AddItem("First section");
        romanList.AddItem("Second section");
        romanList.AddItem("Third section");
        romanList.AddItem("Fourth section");

        doc.Add(romanList);

        // Lowercase letters
        doc.Add(new TextBlock
        {
            Text = "Lowercase Letters (a, b, c...)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        OrderedList alphaList = new()
        {
            NumberFormat = NumberFormat.LowerAlpha,
            StartNumber = 1,
            FontSize = 12,
            ItemSpacing = 8
        };
        alphaList.AddItem("Sub-point one");
        alphaList.AddItem("Sub-point two");
        alphaList.AddItem("Sub-point three");

        doc.Add(alphaList);

        // Custom start number
        doc.Add(new TextBlock
        {
            Text = "List Starting at Number 5",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        OrderedList customStartList = new()
        {
            NumberFormat = NumberFormat.Decimal,
            StartNumber = 5,
            FontSize = 12,
            ItemSpacing = 8
        };
        customStartList.AddItem("Item number 5");
        customStartList.AddItem("Item number 6");
        customStartList.AddItem("Item number 7");

        doc.Add(customStartList);

        // Add a second page for nested lists
        doc.AddPage();

        doc.Add(new TextBlock
        {
            Text = "Nested Ordered Lists",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Main list
        OrderedList mainOrderedList = new()
        {
            NumberFormat = NumberFormat.Decimal,
            StartNumber = 1,
            FontSize = 12,
            ItemSpacing = 8
        };
        mainOrderedList.AddItem("Main item one");
        mainOrderedList.AddItem("Main item two with nested list below");

        // Nested list
        OrderedList nestedOrderedList = new()
        {
            NumberFormat = NumberFormat.LowerAlpha,
            StartNumber = 1,
            FontSize = 12,
            ItemSpacing = 6,
            Indent = 20
        };
        nestedOrderedList.AddItem("Nested item A");
        nestedOrderedList.AddItem("Nested item B");
        nestedOrderedList.AddItem("Nested item C");

        mainOrderedList.AddNestedList(nestedOrderedList);

        mainOrderedList.AddItem("Main item three");
        mainOrderedList.AddItem("Main item four");

        doc.Add(mainOrderedList);

        // ==========================================
        // PAGE BREAK BETWEEN ORDERED AND UNORDERED
        // ==========================================
        doc.AddPage();

        // ==========================================
        // UNORDERED LISTS
        // ==========================================

        // Title
        doc.Add(new TextBlock
        {
            Text = "Unordered List Examples",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Disc bullet style
        doc.Add(new TextBlock
        {
            Text = "Disc Bullet Style (•)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700
        });

        UnorderedList discList = new()
        {
            BulletStyle = BulletStyle.Disc,
            FontSize = 12,
            ItemSpacing = 8
        };
        discList.AddItem("First feature point");
        discList.AddItem("Second feature point with longer text to show wrapping");
        discList.AddItem("Third feature point");
        discList.AddItem("Fourth feature point");

        doc.Add(discList);

        // Circle bullet style
        doc.Add(new TextBlock
        {
            Text = "Circle Bullet Style (◦)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        UnorderedList circleList = new()
        {
            BulletStyle = BulletStyle.Circle,
            FontSize = 12,
            ItemSpacing = 8
        };
        circleList.AddItem("Sub-feature one");
        circleList.AddItem("Sub-feature two");
        circleList.AddItem("Sub-feature three");

        doc.Add(circleList);

        // Square bullet style
        doc.Add(new TextBlock
        {
            Text = "Square Bullet Style (■)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        UnorderedList squareList = new()
        {
            BulletStyle = BulletStyle.Square,
            FontSize = 12,
            ItemSpacing = 8
        };
        squareList.AddItem("Important note one");
        squareList.AddItem("Important note two");
        squareList.AddItem("Important note three");

        doc.Add(squareList);

        // Dash bullet style
        doc.Add(new TextBlock
        {
            Text = "Dash Bullet Style (-)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        UnorderedList dashList = new()
        {
            BulletStyle = BulletStyle.Dash,
            FontSize = 12,
            ItemSpacing = 8
        };
        dashList.AddItem("Quick point one");
        dashList.AddItem("Quick point two");
        dashList.AddItem("Quick point three");

        doc.Add(dashList);

        // Custom bullet style
        doc.Add(new TextBlock
        {
            Text = "Custom Bullet Style (★)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        UnorderedList customList = new()
        {
            BulletStyle = BulletStyle.Custom,
            CustomBullet = "★",
            FontSize = 12,
            ItemSpacing = 8
        };
        customList.AddItem("Starred item one");
        customList.AddItem("Starred item two");
        customList.AddItem("Starred item three");

        doc.Add(customList);

        // Add a second page for nested lists
        doc.AddPage();

        doc.Add(new TextBlock
        {
            Text = "Nested Unordered Lists",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Main list
        UnorderedList mainUnorderedList = new()
        {
            BulletStyle = BulletStyle.Disc,
            FontSize = 12,
            ItemSpacing = 8
        };
        mainUnorderedList.AddItem("Main category one");
        mainUnorderedList.AddItem("Main category two with sub-items");

        // Nested list
        UnorderedList nestedUnorderedList = new()
        {
            BulletStyle = BulletStyle.Circle,
            FontSize = 12,
            ItemSpacing = 6,
            Indent = 20
        };
        nestedUnorderedList.AddItem("Sub-item one");
        nestedUnorderedList.AddItem("Sub-item two");
        nestedUnorderedList.AddItem("Sub-item three");

        mainUnorderedList.AddNestedList(nestedUnorderedList);

        mainUnorderedList.AddItem("Main category three");
        mainUnorderedList.AddItem("Main category four");

        doc.Add(mainUnorderedList);

        // Color customization example
        doc.Add(new TextBlock
        {
            Text = "Colored Bullets",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 0)
        });

        UnorderedList coloredList = new()
        {
            BulletStyle = BulletStyle.Square,
            BulletColor = Color.Red,
            TextColor = Color.Slate600,
            FontSize = 12,
            ItemSpacing = 8
        };
        coloredList.AddItem("Red bullet point");
        coloredList.AddItem("Another red bullet point");
        coloredList.AddItem("Third red bullet point");

        doc.Add(coloredList);

        // Save the document
        doc.Save(outputPath);

        return outputPath;
    }
}