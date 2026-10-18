using BonniePDF;

namespace BonniePDF.Examples.Elements.ShapesAndBorders;

/// <summary>
/// Demonstrates rounded rectangles (corner radius) on containers and bordered elements.
/// Rounded corners are a universal UI element in modern document design,
/// built from four arcs and four lines using Bezier curve approximations.
/// </summary>
[System.ComponentModel.DisplayName("Shapes & Borders")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Drawing basic shapes and styling borders.")]
public static class ShapesAndBordersExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 50
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle =
            new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Rounded Rectangles";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Rounded Rectangles",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 20)
        });

        // --- Section 1: Basic Rounded Containers ---
        doc.Add(CreateSectionHeader("Basic Rounded Containers"));

        // Simple rounded container with background
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 8,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A container with 8pt corner radius and a light blue background.",
                FontSize = 12,
                Color = ColorPalette.Blue[800]
            }
        });

        // Larger radius
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Green[50],
            CornerRadius = 16,
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text =
                    "A container with a larger 16pt corner radius. The arcs become more pronounced as the radius increases.",
                FontSize = 12,
                Color = ColorPalette.Green[800]
            }
        });

        // --- Section 2: Rounded Borders ---
        doc.Add(CreateSectionHeader("Rounded Borders"));

        // Border only (no fill)
        doc.Add(new Container
        {
            Border = new Border(2),
            BorderColors = new BorderColors(ColorPalette.Indigo[500]),
            CornerRadius = 10,
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "A rounded border with no background fill. The stroke follows the curved path.",
                FontSize = 12
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
                Text = "Rounded border combined with a background fill - great for alert boxes and callouts.",
                FontSize = 12,
                Color = Color.FromHex("#92400e")
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

        // Each title is bold 14pt text on its own tint - Blue/Purple[500] and
        // Teal[500] all fall short of 4.5:1 there (3.46/3.84/2.32:1); one step
        // darker clears it in every case.
        cardRow.Add(
            CreateCard("Feature A", "Rounded corners make UI elements feel modern and approachable.",
                ColorPalette.Blue[600], ColorPalette.Blue[50]), 1f);
        cardRow.Add(
            CreateCard("Feature B", "Bezier curves approximate perfect quarter-circle arcs.", ColorPalette.Purple[600],
                Color.FromHex("#faf5ff")), 1f);
        cardRow.Add(
            CreateCard("Feature C", "Works with backgrounds, borders, or both combined.", ColorPalette.Teal[700],
                Color.FromHex("#f0fdfa")), 1f);

        doc.Add(cardRow);

        // --- Section 5: Varying Radii ---
        // The heading and all six samples stay together: a comparison split across pages defeats its purpose.
        VerticalStack radiusSection = new() { BreakAllowed = false };
        radiusSection.Add(CreateSectionHeader("Radius Comparison"));

        int[] radii = { 0, 4, 8, 12, 20, 30 };
        foreach (int r in radii)
        {
            radiusSection.Add(new Container
            {
                BackgroundColor = ColorPalette.Slate[100],
                Border = new Border(1),
                BorderColors = new BorderColors(ColorPalette.Slate[400]),
                CornerRadius = r,
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6),
                Child = new TextBlock
                {
                    Text = r == 0 ? "CornerRadius = 0 (sharp corners, default)" : $"CornerRadius = {r}",
                    FontSize = 11
                }
            });
        }

        doc.Add(radiusSection);

        // --- Section 6: Practical Examples ---
        doc.Add(new Line
        {
            Color = ColorPalette.Slate[300],
            Thickness = 1,
            Margin = new Thickness(0, 16, 0, 0)
        });
        VerticalStack bannerSection = new() { BreakAllowed = false };
        bannerSection.Add(CreateSectionHeader("Practical Examples"));

        // Success banner
        bannerSection.Add(new Container
        {
            BackgroundColor = Color.FromHex("#dcfce7"),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Green[600]),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Success! Your document has been generated successfully.",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                Color = ColorPalette.Green[800]
            }
        });

        // Warning banner
        bannerSection.Add(new Container
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
                Color = Color.FromHex("#854d0e")
            }
        });

        // Error banner
        bannerSection.Add(new Container
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
                Color = Color.FromHex("#991b1b")
            }
        });

        // Info banner
        bannerSection.Add(new Container
        {
            BackgroundColor = Color.FromHex("#dbeafe"),
            Border = new Border(1),
            BorderColors = new BorderColors(ColorPalette.Blue[600]),
            CornerRadius = 8,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = "Info: Rounded corners are built from four Bezier arcs using k=0.5523.",
                FontName = StandardFonts.Helvetica,
                FontSize = 12,
                Color = ColorPalette.Blue[800]
            },
        });
        doc.Add(bannerSection);
        return doc;
    }

    private static Container CreateCard(string title, string description, Color accentColor, Color bgColor)
    {
        VerticalStack content = new();
        content.Add(new TextBlock
        {
            Text = title,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = accentColor,
            Margin = new Thickness(0, 0, 0, 6)
        });
        content.Add(new TextBlock
        {
            Text = description,
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            Color = ColorPalette.Slate[600]
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
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 12, 0, 6)
        };
    }
}
