using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._02_Layout;

/// <summary>
/// Generates a document with a VerticalStack containing many items that flows across pages.
/// </summary>
public class VerticalStackOverflowExample : ExampleBase
{
    public override string Id => "VerticalStackOverflow";
    public override string Title => "VerticalStack Overflow";
    public override string Description => "VerticalStack overflow flow architecture.";
    public override string FileName => "verticalstack_overflow.pdf";
    public override ExampleCategory Category => ExampleCategory.Layout;
    
    public override string Generate(string outputPath)
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "VerticalStack Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates VerticalStack flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Item Catalog",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "A collection of items demonstrating automatic pagination",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create a vertical stack with many items
        VerticalStack stack = new() { Margin = new Thickness(0) };

        for (int i = 1; i <= 75; i++)
        {
            // Each item is a small card-like element
            VerticalStack itemStack = new()
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            itemStack.AddChild(new TextBlock
            {
                Text = $"Item #{i:D3}",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = Color.Blue600,
                Margin = new Thickness(0, 0, 0, 5)
            });

            itemStack.AddChild(new TextBlock
            {
                Text =
                    $"This is the description for item {i}. It contains some details about what this item represents and why it's important.",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Slate600
            });

            stack.AddChild(itemStack);
        }

        doc.Add(stack);

        doc.Save(outputPath);
        return outputPath;
    }
}
