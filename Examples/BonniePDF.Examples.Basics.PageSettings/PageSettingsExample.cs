using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.PageSettings;

[System.ComponentModel.DisplayName("Page Settings")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("Demonstrates orientation, sizes, and backgrounds.")]
public static class PageSettingsExample
{
    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 40,
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken} - BonniePDF Standard Formats",
                FontName = StandardFonts.Helvetica,
                FontSize = 9,
                Color = ColorPalette.Slate[400],
                Alignment = TextAlignment.Center
            }
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Standard PDF Page Formats Gallery";

        var standardSizes = PdfSize.AllSizes.OrderBy(s => s.Name);

        bool isFirstPage = true;

        foreach (var size in standardSizes)
        {
            string formatName = size.Name ?? "Custom";

            // Generate both Portrait and Landscape for each standard
            foreach (var orientation in new[] { PageOrientation.Portrait, PageOrientation.Landscape })
            {
                if (!isFirstPage)
                {
                    doc.AddPage(size, orientation, 40);
                }
                else
                {
                    // Update the first page's settings to match the first property
                    doc.SetCurrentPageSettings(size, orientation, 40);
                    isFirstPage = false;
                }

                PdfSize current = size.GetOrientedSize(orientation);

                // 3. Build a "Spec Card" for each page
                var content = new VerticalStack();

                content.Add(new TextBlock
                {
                    Text = $"{formatName} ({orientation})",
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 28,
                    Color = ColorPalette.Slate[800]
                });

                content.Add(new TextBlock { Text = "Technical Specifications", FontSize = 14, Color = ColorPalette.Slate[500] });
                content.Add(new Line { Color = ColorPalette.Slate[200], Thickness = 1 });

                // Data Table Simulation
                content.Add(CreateSpecLine("Points:", $"{current.Width:F2}pt x {current.Height:F2}pt"));
                content.Add(CreateSpecLine("Inches:", $"{current.Width / 72:F2}\" x {current.Height / 72:F2}\""));
                content.Add(CreateSpecLine("Millimeters:", $"{(current.Width / 72 * 25.4f):F1}mm x {(current.Height / 72 * 25.4f):F1}mm"));

                doc.Add(content);
            }
        }
        return doc;
    }

    private static TextBlock CreateSpecLine(string label, string value)
    {
        return new TextBlock
        {
            Text = $"{label.PadRight(15)} {value}",
            FontName = StandardFonts.Courier, // Monospace for alignment
            FontSize = 11,
            Color = ColorPalette.Slate[600]
        };
    }
}

