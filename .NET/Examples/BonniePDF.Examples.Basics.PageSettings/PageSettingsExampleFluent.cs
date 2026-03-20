using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.PageSettings;

/// <summary>
/// Demonstrates orientation, page sizes, and backgrounds using the Fluent API.
/// Iterates through every standard PDF size in both Portrait and Landscape orientations,
/// producing a "spec card" page for each via repeated <c>doc.Page()</c> calls inside a loop.
/// The footer template is registered once via <c>settings.Footer()</c> and applies to all pages.
/// </summary>
[System.ComponentModel.DisplayName("Page Settings (Fluent API)")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("Demonstrates orientation, sizes, and backgrounds using the Fluent API.")]
public static class PageSettingsExampleFluent
{
    /// <summary>Both orientations rendered for each standard page size.</summary>
    private static readonly PageOrientation[] Orientations =
        [PageOrientation.Portrait, PageOrientation.Landscape];

    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Standard PDF Page Formats Gallery (Fluent API)";
            });

            doc.WithSettings(settings =>
            {
                settings.Version     = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;

                // Running footer: centred page number and document name on every page
                settings.Footer((pageNumber, footer) =>
                {
                    footer.Text($"Page {pageNumber} - BonniePDF Standard Formats")
                        .Font(StandardFonts.Helvetica)
                        .FontSize(9)
                        .TextColor(Color.Slate400)
                        .AlignCenter();
                });
            });

            // One doc.Page() call per size × orientation combination.
            // The handler executes synchronously, so loop variables are captured correctly.
            foreach (PdfSize size in PdfSize.AllSizes.OrderBy(s => s.Name))
            {
                foreach (PageOrientation orientation in Orientations)
                {
                    PdfSize current    = size.GetOrientedSize(orientation);
                    string  formatName = size.Name ?? "Custom";

                    doc.Page(page =>
                    {
                        page.Size(size);
                        page.Orientation(orientation);
                        page.Margin(40);

                        page.Content().Column(col =>
                        {
                            // Page title: "A4 (Portrait)" / "A4 (Landscape)" etc.
                            col.Item()
                                .Text($"{formatName} ({orientation})")
                                .Font(StandardFonts.HelveticaBold)
                                .FontSize(28)
                                .TextColor(Color.Slate800)
                                .Margin(0, 0, 0, 4);

                            col.Item()
                                .Text("Technical Specifications")
                                .Font(StandardFonts.Helvetica)
                                .FontSize(14)
                                .TextColor(Color.Slate500)
                                .Margin(0, 0, 0, 4);

                            col.Item().Line(1, Color.Slate200);

                            // Spec rows in monospace for alignment
                            col.Item()
                                .Text($"{"Points:".PadRight(15)} {current.Width:F2}pt x {current.Height:F2}pt")
                                .Font(StandardFonts.Courier)
                                .FontSize(11)
                                .TextColor(Color.Slate600)
                                .Margin(0, 6, 0, 0);

                            col.Item()
                                .Text($"{"Inches:".PadRight(15)} {current.Width / 72:F2}\" x {current.Height / 72:F2}\"")
                                .Font(StandardFonts.Courier)
                                .FontSize(11)
                                .TextColor(Color.Slate600);

                            col.Item()
                                .Text($"{"Millimeters:".PadRight(15)} {current.Width / 72 * 25.4f:F1}mm x {current.Height / 72 * 25.4f:F1}mm")
                                .Font(StandardFonts.Courier)
                                .FontSize(11)
                                .TextColor(Color.Slate600);
                        });
                    });
                }
            }

        }).Build();

    }
}

