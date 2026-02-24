using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples;

/// <summary>
/// Demonstrates text watermarks rendered behind page content.
/// Watermarks use PDF ExtGState for opacity and the cm operator for rotation,
/// appearing as semi-transparent diagonal text behind all page elements.
/// </summary>
public class WatermarkExample
{
    /// <summary>
    /// Generates a multipage document with a "DRAFT" watermark on every page.
    /// </summary>
    public static string GenerateDraft(string outputPath = "watermark_draft.pdf")
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Draft Document with Watermark";
        doc.Metadata.Author = "BonniePDF";

        // Apply the watermark — default settings: gray, 15% opacity, -45 degrees, 72pt
        doc.Settings.Watermark = new WatermarkSettings { Text = "DRAFT" };

        // Page 1: Title page
        doc.Add(new TextBlock
        {
            Text = "Project Proposal",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 80, 0, 12)
        }.AlignCenter());

        doc.Add(new TextBlock
        {
            Text = "Prepared for Internal Review",
            FontName = StandardFonts.Helvetica,
            FontSize = 14,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 0, 0, 40)
        }.AlignCenter());

        doc.Add(new TextBlock
        {
            Text = "This document is a draft and has not been approved for distribution. " +
                   "All content is subject to change pending final review by the project committee. " +
                   "Please do not share this document outside the review team.",
            FontName = StandardFonts.TimesRoman,
            FontSize = 12,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Justify
        });

        // Page 2: Some content
        doc.AddPage();
        doc.Add(new TextBlock
        {
            Text = "Section 1: Executive Summary",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "The proposed initiative aims to modernize the existing infrastructure by adopting " +
                   "cloud-native technologies and microservice architectures. This transformation will " +
                   "enable greater scalability, improved fault tolerance, and faster deployment cycles. " +
                   "The estimated timeline for Phase 1 completion is twelve months, with a projected " +
                   "budget allocation of two million dollars across engineering and operations teams.",
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Alignment = TextAlignment.Justify,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "Key stakeholders include the VP of Engineering, the CTO, and department leads " +
                   "from Product, Security, and Operations. A steering committee will convene monthly " +
                   "to review progress against milestones and address any blockers or scope changes.",
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate700,
            Alignment = TextAlignment.Justify
        });

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Generates a document with a "CONFIDENTIAL" watermark using custom styling.
    /// </summary>
    public static string GenerateConfidential(string outputPath = "watermark_confidential.pdf")
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Confidential Report";
        doc.Metadata.Author = "BonniePDF";

        // Custom watermark: red, larger, steeper angle
        doc.Settings.Watermark = new WatermarkSettings
        {
            Text = "CONFIDENTIAL",
            FontSize = 60,
            TextColor = Color.Red,
            Opacity = 0.08f,
            RotationDegrees = -55
        };

        doc.Add(new TextBlock
        {
            Text = "Financial Performance Report Q4 2025",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 22,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Summary table
        Table table = new(new float[] { 200, 120, 120 });

        // Header row
        table.AddRow(
            CreateHeaderCell("Metric"),
            CreateHeaderCell("Q3 2025"),
            CreateHeaderCell("Q4 2025")
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
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 20, 0, 0)
        });

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Demonstrates the fluent API for adding watermarks.
    /// </summary>
    public static string GenerateFluent(string outputPath = "watermark_fluent.pdf")
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

    private static TableCell CreateHeaderCell(string text)
    {
        return new TableCell
        {
            Content = new TextBlock
            {
                Text = text,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 11,
                TextColor = Color.White
            },
            BackgroundColor = Color.Slate700,
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
                TextColor = Color.Slate700
            },
            Padding = new Thickness(8, 5, 8, 5),
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Color.Slate200)
        };
    }
}
