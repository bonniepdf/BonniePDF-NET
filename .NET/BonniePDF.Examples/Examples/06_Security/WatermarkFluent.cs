using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Examples.Core;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._06_Security;

/// <summary>
/// Demonstrates the fluent API for adding watermarks.
/// Watermarks use PDF ExtGState for opacity and the cm operator for rotation,
/// appearing as semi-transparent diagonal text behind all page elements.
/// </summary>
public class WatermarkFluentExample : ExampleBase
{
    public override string Id => "watermark-fluent";
    public override string Title => "Watermark - Fluent API";
    public override string Description => "Demonstrates the fluent API for adding watermarks with a simple one-liner configuration.";
    public override string FileName => "watermark_fluent.pdf";
    public override ExampleCategory Category => ExampleCategory.Security;

    public override string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Watermark Fluent API";
        doc.Metadata.Author = "BonniePDF";

        // Fluent API: simple one-liner
        doc.Settings.WithWatermark("SAMPLE", opacity: 0.12f, rotationDegrees: -40);

        doc.Add(new TextBlock
        {
            Text = "Fluent Watermark API",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 16)
        });

        doc.Add(new TextBlock
        {
            Text = "This document demonstrates the fluent API for adding watermarks. " +
                   "A single call to Settings.WithWatermark(\"SAMPLE\", opacity: 0.12f, " +
                   "rotationDegrees: -40) configures the watermark with custom opacity and rotation.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Justify,
            Margin = new Thickness(0, 0, 0, 16)
        });

        doc.Add(new TextBlock
        {
            Text = "The watermark appears behind all content on every page, using PDF " +
                   "ExtGState for transparency and the cm operator for rotation. The text " +
                   "is centered on each page and automatically sized to fit diagonally.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Justify
        });

        doc.Save(outputPath);
        return outputPath;
    }
}
