using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Security.Watermarking;

/// <summary>
/// Demonstrates the fluent API for adding watermarks.
/// Watermarks use PDF ExtGState for opacity and the cm operator for rotation,
/// appearing as semi-transparent diagonal text behind all page elements.
/// </summary>
public static class WatermarkFluentExample
{

    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Watermark Fluent API";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;

                // Fluent API: simple one-liner
                settings.WithWatermark("SAMPLE", opacity: 0.12f, rotationDegrees: -40);
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(12));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    column.Item()
                        .Text("Fluent Watermark API")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 16);

                    column.Item()
                        .Text("This document demonstrates the fluent API for adding watermarks. " +
                              "A single call to Settings.WithWatermark(\"SAMPLE\", opacity: 0.12f, " +
                              "rotationDegrees: -40) configures the watermark with custom opacity and rotation.")
                        .TextColor(Color.Slate600)
                        .AlignJustify()
                        .Margin(0, 0, 0, 16);

                    column.Item()
                        .Text("The watermark appears behind all content on every page, using PDF " +
                              "ExtGState for transparency and the cm operator for rotation. The text " +
                              "is centered on each page and automatically sized to fit diagonally.")
                        .TextColor(Color.Slate600)
                        .AlignJustify();
                });
            });
        }).Save(outputPath);

        return outputPath;
    }
}
