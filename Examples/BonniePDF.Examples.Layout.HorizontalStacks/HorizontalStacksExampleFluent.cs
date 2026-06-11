using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HorizontalStacks;

/// <summary>
/// Demonstrates the HorizontalStack element for horizontal layouts using the Fluent API.
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Stacks (Fluent API)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Demonstrates flex-like horizontal layout blocks using the Fluent API.")]
public static class HorizontalStacksExampleFluent
{
    /// <summary>
    /// Generates the horizontal stacks example PDF using the Fluent API.
    /// The document illustrates equal-width, weighted, and badge-style row layouts.
    /// </summary>
    /// <returns>The path of the generated PDF file.</returns>
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Horizontal Stack Example";
                metadata.Author = "BonniePDF";
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(12));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Horizontal Stack Examples")
                        .Bold()
                        .FontSize(24)
                        .TextColor(ColorPalette.Slate[800])
                        .Margin(0, 0, 0, 12);

                    // Equal width distribution
                    column.Item()
                        .Text("Equal Width Distribution")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 0, 0, 8);

                    // Wrap in container with background color, then equal-width row
                    column.Item()
                        .Padding(10)
                        .Background(ColorPalette.Slate[200])
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Text("Item 1").TextColor(Color.White);
                            row.RelativeItem().Text("Item 2").TextColor(Color.White);
                            row.RelativeItem().Text("Item 3").TextColor(Color.White);
                        });

                    // Weighted width distribution
                    column.Item()
                        .Text("Weighted Width Distribution (1:2:3 ratio)")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Padding(10)
                        .Background(ColorPalette.Slate[200])
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("25%").TextColor(Color.White);
                            row.RelativeItem(2f).Text("50%").TextColor(Color.White);
                            row.RelativeItem(3f).Text("75%").TextColor(Color.White);
                        });

                    // Center alignment
                    column.Item()
                        .Text("Center Vertical Alignment")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Height(160)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.AlignItems(HorizontalAlignment.Center);
                            row.RelativeItem().Text("Small").FontSize(10);
                            row.RelativeItem().Text("Medium\n(2 lines)");
                            row.RelativeItem().Text("Large\n(3 lines)\nof text").FontSize(14);
                        });

                    // Colored containers in horizontal stack
                    column.Item()
                        .Text("Colored Containers")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(5);
                            row.RelativeItem().Padding(15).Background(Color.Red).Text("Red").Bold().FontSize(14).TextColor(Color.White);
                            row.RelativeItem().Padding(15).Background(ColorPalette.Green[600]).Text("Green").Bold().FontSize(14).TextColor(Color.White);
                            row.RelativeItem().Padding(15).Background(Color.Blue).Text("Blue").Bold().FontSize(14).TextColor(Color.White);
                        });

                    // Practical Examples
                    column.Item()
                        .Text("Practical Examples")
                        .Bold()
                        .FontSize(24)
                        .TextColor(ColorPalette.Slate[800])
                        .Margin(0, 0, 0, 12);

                    // Key-value pairs
                    column.Item()
                        .Text("Key-Value Pairs")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 0, 0, 8);

                    var kvSource = Placeholders.WithSeed(300);
                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("Name:").Bold().TextColor(ColorPalette.Slate[600]);
                            row.RelativeItem(3f).Text(kvSource.Name()).TextColor(ColorPalette.Slate[800]);
                        });

                    column.Item()
                        .Margin(0, 8, 0, 0)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("Email:").Bold().TextColor(ColorPalette.Slate[600]);
                            row.RelativeItem(3f).Text(kvSource.Email()).TextColor(ColorPalette.Slate[800]);
                        });

                    // Progress bar style
                    column.Item()
                        .Text("Progress Bar Style")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(0);
                            row.RelativeItem(75f).Padding(10, 5, 10, 5).Background(ColorPalette.Green[600]).Height(24)
                                .Text("75%").Bold().TextColor(Color.White);
                            row.RelativeItem(25f).Padding(10, 5, 10, 5).Background(ColorPalette.Slate[300]).Height(24).Empty();
                        });

                    // Badge style
                    column.Item()
                        .Text("Badge Style Layout")
                        .Bold()
                        .FontSize(14)
                        .TextColor(ColorPalette.Slate[700])
                        .Margin(0, 16, 0, 8);

                    // NOTE: HorizontalStack.Alignment (HorizontalAlignment.Center) has no direct
                    // fluent equivalent. Auto-width distribution is approximated with row.AutoItem().
                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.AutoItem().Text("Status:").TextColor(ColorPalette.Slate[600]);
                            row.AutoItem().Padding(8, 4, 8, 4).Background(ColorPalette.Green[600]).Text("Active").Bold().FontSize(11).TextColor(Color.White);
                            row.AutoItem().Padding(8, 4, 8, 4).Background(Color.Blue).Text("Verified").Bold().FontSize(11).TextColor(Color.White);
                        });
                });
            });
        }).Build();

    }
}

