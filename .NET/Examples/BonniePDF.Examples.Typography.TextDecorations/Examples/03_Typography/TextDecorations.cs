using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._03_Typography;

/// <summary>
/// Demonstrates text decorations: underline and strikethrough.
/// Decorations can be applied individually or combined, and work
/// with all text alignments including justified text.
/// </summary>
public static class TextDecorationExample
{
    
    public static string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Text Decorations";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Text Decorations",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // --- Underline ---
        doc.Add(CreateSectionHeader("Underline"));

        doc.Add(new TextBlock
        {
            Text = "This text has an underline decoration applied.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Underline,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Underline with fluent API
        doc.Add(new TextBlock
        {
            Text = "This underline was applied using the fluent .Underline() extension method.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 12)
        }.Underline());

        // --- Strikethrough ---
        doc.Add(CreateSectionHeader("Strikethrough"));

        doc.Add(new TextBlock
        {
            Text = "This text has a strikethrough decoration, useful for showing deleted or superseded content.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Strikethrough,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This strikethrough was applied using the fluent .Strikethrough() extension method.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 12)
        }.Strikethrough());

        // --- Combined ---
        doc.Add(CreateSectionHeader("Combined Decorations"));

        doc.Add(new TextBlock
        {
            Text = "This text has both underline and strikethrough applied simultaneously.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Underline | TextDecoration.Strikethrough,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Combined with fluent API (additive)
        doc.Add(new TextBlock
        {
            Text = "Combined decorations via fluent API: .Underline().Strikethrough() chains are additive.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 12)
        }.Underline().Strikethrough());

        // --- Decorations with different alignments ---
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1 });
        doc.Add(CreateSectionHeader("Decorations with Different Alignments"));

        doc.Add(new TextBlock
        {
            Text = "Left-aligned underlined text",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Underline,
            Alignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Add(new TextBlock
        {
            Text = "Center-aligned underlined text",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Underline,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Add(new TextBlock
        {
            Text = "Right-aligned underlined text",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            TextDecoration = TextDecoration.Underline,
            Alignment = TextAlignment.Right,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // --- Decorations with different font sizes ---
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1 });
        doc.Add(CreateSectionHeader("Decoration Thickness Scales with Font Size"));

        int[] sizes = { 8, 12, 18, 24, 36 };
        foreach (int size in sizes)
        {
            doc.Add(new TextBlock
            {
                Text = $"Underline at {size}pt",
                FontName = StandardFonts.Helvetica,
                FontSize = size,
                TextColor = Color.Slate700,
                TextDecoration = TextDecoration.Underline,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        // --- Practical use case: price comparison ---
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1 });
        doc.Add(CreateSectionHeader("Practical Example: Price List"));

        VerticalStack priceList = new();

        // Original price (struck through)
        priceList.AddChild(new TextBlock
        {
            Text = "Original Price: $199.99",
            FontName = StandardFonts.Helvetica,
            FontSize = 14,
            TextColor = Color.Slate400,
            TextDecoration = TextDecoration.Strikethrough,
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Sale price (underlined for emphasis)
        priceList.AddChild(new TextBlock
        {
            Text = "Sale Price: $149.99",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.Red,
            TextDecoration = TextDecoration.Underline,
            Margin = new Thickness(0, 0, 0, 4)
        });

        priceList.AddChild(new TextBlock
        {
            Text = "You save $50.00!",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Green600
        });

        doc.Add(priceList);

        doc.Save(outputPath);
        return outputPath;
    }

    private static TextBlock CreateSectionHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Indigo600,
            Margin = new Thickness(0, 12, 0, 6)
        };
    }
}
