using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._02_Layout;

/// <summary>
/// Demonstrates the HorizontalStack element for horizontal layouts using the Fluent API.
/// </summary>
public class HorizontalStackExampleFluent : ExampleBase
{
    /// <inheritdoc/>
    public override string Id => "HorizontalStacksFluent";
    /// <inheritdoc/>
    public override string Title => "Horizontal Stacks (Fluent API)";
    /// <inheritdoc/>
    public override string Description => "Demonstrates flex-like horizontal layout blocks using the Fluent API.";
    /// <inheritdoc/>
    public override string FileName => "horizontal_stack_example_fluent.pdf";
    /// <inheritdoc/>
    public override ExampleCategory Category => ExampleCategory.Layout;

    /// <summary>
    /// Generates the horizontal stacks example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates equal-width, weighted, and badge-style
    /// row layouts.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public override string Generate(string outputPath)
    {
        Pdf.Create(doc =>
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

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Horizontal Stack Examples")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    // Equal width distribution
                    column.Item()
                        .Text("Equal Width Distribution")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 8);

                    // Wrap in container with background color, then equal-width row
                    column.Item()
                        .Padding(10)
                        .Background(Color.Slate200)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Text("Item 1").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                            row.RelativeItem().Text("Item 2").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                            row.RelativeItem().Text("Item 3").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                        });

                    // Weighted width distribution
                    column.Item()
                        .Text("Weighted Width Distribution (1:2:3 ratio)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Padding(10)
                        .Background(Color.Slate200)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("25%").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                            row.RelativeItem(2f).Text("50%").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                            row.RelativeItem(3f).Text("75%").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.White);
                        });

                    // Center alignment
                    column.Item()
                        .Text("Center Vertical Alignment")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Height(160)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.AlignItems(HorizontalAlignment.Center);
                            row.RelativeItem().Text("Small").Font(StandardFonts.Helvetica).FontSize(10);
                            row.RelativeItem().Text("Medium\n(2 lines)").Font(StandardFonts.Helvetica).FontSize(12);
                            row.RelativeItem().Text("Large\n(3 lines)\nof text").Font(StandardFonts.Helvetica).FontSize(14);
                        });

                    // Colored containers in horizontal stack
                    column.Item()
                        .Text("Colored Containers")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(5);
                            row.RelativeItem().Padding(15).Background(Color.Red).Text("Red").Font(StandardFonts.HelveticaBold).FontSize(14).TextColor(Color.White);
                            row.RelativeItem().Padding(15).Background(Color.Green600).Text("Green").Font(StandardFonts.HelveticaBold).FontSize(14).TextColor(Color.White);
                            row.RelativeItem().Padding(15).Background(Color.Blue).Text("Blue").Font(StandardFonts.HelveticaBold).FontSize(14).TextColor(Color.White);
                        });

                    // Practical Examples
                    column.Item()
                        .Text("Practical Examples")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    // Key-value pairs
                    column.Item()
                        .Text("Key-Value Pairs")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 8);

                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("Name:").Font(StandardFonts.HelveticaBold).FontSize(12).TextColor(Color.Slate600);
                            row.RelativeItem(3f).Text("John Doe").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.Slate800);
                        });

                    column.Item()
                        .Margin(0, 8, 0, 0)
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem(1f).Text("Email:").Font(StandardFonts.HelveticaBold).FontSize(12).TextColor(Color.Slate600);
                            row.RelativeItem(3f).Text("john.doe@example.com").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.Slate800);
                        });

                    // Progress bar style
                    column.Item()
                        .Text("Progress Bar Style")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 8);

                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(0);
                            row.RelativeItem(75f).Padding(10, 5, 10, 5).Background(Color.Green600).Height(24)
                                .Text("75%").Font(StandardFonts.HelveticaBold).FontSize(12).TextColor(Color.White);
                            row.RelativeItem(25f).Padding(10, 5, 10, 5).Background(Color.Slate300).Height(24).Empty();
                        });

                    // Badge style
                    column.Item()
                        .Text("Badge Style Layout")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 8);

                    // NOTE: HorizontalStack.Alignment (HorizontalAlignment.Center) has no direct
                    // fluent equivalent. Auto-width distribution is approximated with row.AutoItem().
                    column.Item()
                        .Row(row =>
                        {
                            row.Spacing(10);
                            row.AutoItem().Text("Status:").Font(StandardFonts.Helvetica).FontSize(12).TextColor(Color.Slate600);
                            row.AutoItem().Padding(8, 4, 8, 4).Background(Color.Green600).Text("Active").Font(StandardFonts.HelveticaBold).FontSize(11).TextColor(Color.White);
                            row.AutoItem().Padding(8, 4, 8, 4).Background(Color.Blue).Text("Verified").Font(StandardFonts.HelveticaBold).FontSize(11).TextColor(Color.White);
                        });
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
