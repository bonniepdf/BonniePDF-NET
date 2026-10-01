using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Security.Watermarking;

/// <summary>
/// Demonstrates a "CONFIDENTIAL" watermark with custom styling.
/// </summary>
[System.ComponentModel.DisplayName("Watermark - Confidential")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description("Generates a document with a 'CONFIDENTIAL' watermark using custom styling (red color, larger size, steeper angle).")]
public static class WatermarkConfidentialExample
{

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Confidential Report";
        doc.Metadata.Author = "BonniePDF";

        // Custom watermark: red, larger, steeper angle
        doc.Settings.Watermark = new Watermark
        {
            Text = "CONFIDENTIAL",
            FontSize = 60,
            Color = Color.Red,
            Opacity = 0.08f,
            RotationDegrees = -55
        };

        doc.Add(new TextBlock
        {
            Text = $"Financial Performance Report Q4 {DateTime.Now.Year}",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Summary table
        Table table = new(new float[] { 200, 120, 120 });

        // Header row (real tabular data → AddHeader emits TH cells for assistive tech)
        table.AddHeader(
            CreateHeaderCell("Metric"),
            CreateHeaderCell($"Q3 {DateTime.Now.Year}"),
            CreateHeaderCell($"Q4 {DateTime.Now.Year}")
        );

        // Data rows
        table.AddRow(CreateCell("Revenue"), CreateCell("$4.2M"), CreateCell("$5.1M"));
        table.AddRow(CreateCell("Operating Costs"), CreateCell("$2.8M"), CreateCell("$2.9M"));
        table.AddRow(CreateCell("Net Profit"), CreateCell("$1.4M"), CreateCell("$2.2M"));
        table.AddRow(CreateCell("Headcount"), CreateCell("142"), CreateCell("158"));
        table.AddRow(CreateCell("Customer Growth"), CreateCell("+12%"), CreateCell("+18%"));

        doc.Add(table);

        doc.Add(new TextBlock
        {
            Text = "This report contains proprietary financial data and is intended solely for " +
                   "authorized recipients. Unauthorized disclosure or distribution is prohibited.",
            FontName = StandardFonts.TimesItalic,
            FontSize = 10,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 20, 0, 0)
        });
        return doc;
    }

    private static TableCell CreateHeaderCell(string text)
    {
        return new TableCell
        {
            Content = new TextBlock
            {
                Text = text,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 11,
                Color = Color.White
            },
            BackgroundColor = ColorPalette.Slate[700],
            Padding = new Thickness(8, 6, 8, 6)
        };
    }

    private static TableCell CreateCell(string text)
    {
        return new TableCell
        {
            Content = new TextBlock
            {
                Text = text,
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
                Color = ColorPalette.Slate[600]
            },
            Padding = new Thickness(8, 5, 8, 5),
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[200])
        };
    }
}

