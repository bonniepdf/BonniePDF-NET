using System.Linq;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._01_Basics;

public static class AllPagesExample
{
    
    public static string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 40,
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} - BonniePDF Standard Formats",
                FontName = StandardFonts.Helvetica,
                FontSize = 9,
                TextColor = Color.Slate400,
                Alignment = TextAlignment.Center
            }
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

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

                content.AddChild(new TextBlock
                {
                    Text = $"{formatName} ({orientation})",
                    FontName = StandardFonts.HelveticaBold,
                    FontSize = 28,
                    TextColor = Color.Slate800
                });

                content.AddChild(new TextBlock { Text = "Technical Specifications", FontSize = 14, TextColor = Color.Slate500 });
                content.AddChild(new Line { Color = Color.Slate200, Thickness = 1 });

                // Data Table Simulation
                content.AddChild(CreateSpecLine("Points:", $"{current.Width:F2}pt x {current.Height:F2}pt"));
                content.AddChild(CreateSpecLine("Inches:", $"{current.Width / 72:F2}\" x {current.Height / 72:F2}\""));
                content.AddChild(CreateSpecLine("Millimeters:", $"{(current.Width / 72 * 25.4f):F1}mm x {(current.Height / 72 * 25.4f):F1}mm"));

                doc.Add(content);
            }
        }

        doc.Save(outputPath);
        return outputPath;
    }

    private static TextBlock CreateSpecLine(string label, string value)
    {
        return new TextBlock
        {
            Text = $"{label.PadRight(15)} {value}",
            FontName = StandardFonts.Courier, // Monospace for alignment
            FontSize = 11,
            TextColor = Color.Slate600
        };
    }
}
