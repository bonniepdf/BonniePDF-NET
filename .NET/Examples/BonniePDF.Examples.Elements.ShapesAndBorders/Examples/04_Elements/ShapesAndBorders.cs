using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._04_Elements;

/// <summary>
/// Demonstrates rounded rectangles (corner radius) on containers and bordered elements.
/// Rounded corners are a universal UI element in modern document design,
/// built from four arcs and four lines using Bezier curve approximations.
/// </summary>
public static class RoundedRectangleExample
{
    
    public static string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 50
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Rounded Rectangles";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Rounded Rectangles",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // --- Section 1: Basic Rounded Containers ---
        doc.Add(CreateSectionHeader("Basic Rounded Containers"));

        // Simple rounded container with background
        doc.Add(new Container
        {
            BackgroundColor = Color.Blue50,
            CornerRadius = 8,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A container with 8pt corner radius and a light blue background.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                TextColor = Color.Blue800
            }
        });

        // Larger radius
        doc.Add(new Container
        {
            BackgroundColor = Color.Green50,
            CornerRadius = 16,
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A container with a larger 16pt corner radius. The arcs become more pronounced as the radius increases.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                TextColor = Color.Green800
            }
        });

        // --- Section 2: Rounded Borders ---
        doc.Add(CreateSectionHeader("Rounded Borders"));

        // Border only (no fill)
        doc.Add(new Container
        {
            Border = new Border(2),
            BorderColors = new BorderColors(Color.Indigo500),
            CornerRadius = 10,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A rounded border with no background fill. The stroke follows the curved path.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                TextColor = Color.Slate700
            }
        });

        // Border + background combined
        doc.Add(new Container
        {
            BackgroundColor = Color.FromHex("#fef3c7"),
            Border = new Border(2),
            BorderColors = new BorderColors(Color.FromHex("#f59e0b")),
            CornerRadius = 10,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "Rounded border combined with a background fill — great for alert boxes and callouts.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                TextColor = Color.FromHex("#92400e")
            }
        });

        // --- Section 3: Card Layout ---
        doc.Add(CreateSectionHeader("Card Layout"));

        HorizontalStack cardRow = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 12)
        };

        cardRow.AddChild(CreateCard("Feature A", "Rounded corners make UI elements feel modern and approachable.", Color.Blue500, Color.Blue50), 1f);
        cardRow.AddChild(CreateCard("Feature B", "Bezier curves approximate perfect quarter-circle arcs.", Color.Purple500, Color.FromHex("#faf5ff")), 1f);
        cardRow.AddChild(CreateCard("Feature C", "Works with backgrounds, borders, or both combined.", Color.Teal500, Color.FromHex("#f0fdfa")), 1f);

        doc.Add(cardRow);

        // --- Section 4: Fluent API ---
        doc.Add(CreateSectionHeader("Fluent API (.Rounded())"));

        doc.Add(new TextBlock
        {
            Text = "This element uses the fluent .Rounded() extension to apply corner radius via method chaining.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700
        }.Padding(16).Background(Color.FromHex("#ede9fe")).Rounded(12).Border(Color.FromHex("#8b5cf6"), 1));

        doc.Add(new Container { Margin = new Thickness(0, 12, 0, 0) });

        doc.Add(new TextBlock
        {
            Text = "Chaining: .Padding(16).Background(...).Rounded(12).Border(...)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // --- Section 5: Varying Radii ---
        doc.Add(CreateSectionHeader("Radius Comparison"));

        int[] radii = { 0, 4, 8, 12, 20, 30 };
        foreach (int r in radii)
        {
            doc.Add(new Container
            {
                BackgroundColor = Color.Slate100,
                Border = new Border(1),
                BorderColors = new BorderColors(Color.Slate400),
                CornerRadius = r,
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6),
                Child = new TextBlock
                {
                    Text = r == 0 ? "CornerRadius = 0 (sharp corners, default)" : $"CornerRadius = {r}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 11,
                    TextColor = Color.Slate700
                }
            });
        }

        // --- Section 6: Practical Examples ---
        doc.Add(new Line { Color = Color.Slate300, Thickness = 1, Margin = new Thickness(0, 16, 0, 0) });
        doc.Add(CreateSectionHeader("Practical Examples"));

        // Success banner
        doc.Add(new Container
        {
            BackgroundColor = Color.FromHex("#dcfce7"),
            Border = new Border(1),
            BorderColors = new BorderColors(Color.Green600),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Success! Your document has been generated successfully.",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = Color.Green800
            }
        });

        // Warning banner
        doc.Add(new Container
        {
            BackgroundColor = Color.FromHex("#fef9c3"),
            Border = new Border(1),
            BorderColors = new BorderColors(Color.FromHex("#ca8a04")),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Warning: Please review the document before sending.",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = Color.FromHex("#854d0e")
            }
        });

        // Error banner
        doc.Add(new Container
        {
            BackgroundColor = Color.FromHex("#fee2e2"),
            Border = new Border(1),
            BorderColors = new BorderColors(Color.FromHex("#991b1b")),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Error: Unable to process the request. Please try again.",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = Color.FromHex("#991b1b")
            }
        });

        // Info banner
        doc.Add(new Container
        {
            BackgroundColor = Color.FromHex("#dbeafe"),
            Border = new Border(1),
            BorderColors = new BorderColors(Color.Blue600),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Info: Rounded corners are built from four Bezier arcs using the k=0.5523 approximation constant.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                TextColor = Color.Blue800
            },
        });

        doc.Save(outputPath);
        return outputPath;
    }

    private static Container CreateCard(string title, string description, Color accentColor, Color bgColor)
    {
        VerticalStack content = new();
        content.AddChild(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = accentColor,
            Margin = new Thickness(0, 0, 0, 6)
        });
        content.AddChild(new TextBlock
        {
            Text = description,
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Slate600
        });

        return new Container
        {
            BackgroundColor = bgColor,
            Border = new Border(1),
            BorderColors = new BorderColors(accentColor),
            CornerRadius = 10,
            Padding = new Thickness(14),
            Child = content
        };
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
