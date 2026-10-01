using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Security.Watermarking;

/// <summary>
/// Demonstrates a "DRAFT" watermark rendered over page content.
/// Watermarks use PDF ExtGState for opacity and the cm operator for rotation,
/// appearing as semi-transparent diagonal text over all page elements.
/// </summary>
[System.ComponentModel.DisplayName("Watermark - Draft")]
[System.ComponentModel.Category("Security")]
[System.ComponentModel.Description("Generates a multipage document with a 'DRAFT' watermark on every page, demonstrating document status marking.")]
public static class WatermarkDraftExample
{

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 64
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Draft Document with Watermark";
        doc.Metadata.Author = "BonniePDF";

        // A tuned watermark: a large, faint diagonal DRAFT sits over the content
        // on every page. Watermark exposes the font, size, colour, opacity
        // and rotation so the stamp can be matched to the document's palette.
        doc.Settings.Watermark = new Watermark
        {
            Text = "DRAFT",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 120,
            Color = ColorPalette.Slate[400],
            Opacity = 0.12f,
            RotationDegrees = -45
        };

        // ── Page 1: Title page ────────────────────────────────────────────────

        // Brand banner - a filled container introduces the document above the stamp.
        VerticalStack bannerStack = new();
        bannerStack.Add(new TextBlock
        {
            Text = "SAMPLE COMPANY",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = ColorPalette.Blue[200],
            Margin = new Thickness(0, 0, 0, 6)
        });
        bannerStack.Add(new TextBlock
        {
            Text = "Product Strategy Proposal",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            Color = ColorPalette.Slate[50]
        });

        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Blue[700],
            CornerRadius = 10,
            Padding = new Thickness(28, 26, 28, 26),
            Margin = new Thickness(0, 0, 0, 24),
            Child = bannerStack
        });

        // Draft-status callout - a tinted box that explains the watermark.
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Amber[50],
            Border = Border.LeftOnly(4),
            BorderColors = new BorderColors(ColorPalette.Amber[500]),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 24),
            Child = new TextBlock
            {
                Text = "This document is a draft and has not been approved for distribution. " +
                       "All content is subject to change pending final review by the project committee. " +
                       "Please do not share this document outside the review team.",
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
                Color = ColorPalette.Amber[800],
                Alignment = TextAlignment.Justify
            }
        });

        doc.Add(new TextBlock
        {
            Text = "Prepared for internal review by the strategy team. The sections that follow outline " +
                   "the proposed initiative, its objectives, and the resourcing required to deliver it.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Justify
        });

        // ── Page 2: Content page ──────────────────────────────────────────────
        doc.AddPage();
        doc.Add(new TextBlock
        {
            Text = "Section 1: Executive Summary",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = ColorPalette.Blue[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "The proposed initiative aims to modernize the existing infrastructure by adopting " +
                   "cloud-native technologies and microservice architectures. This transformation will " +
                   "enable greater scalability, improved fault tolerance, and faster deployment cycles. " +
                   "The estimated timeline for Phase 1 completion is twelve months, with a projected " +
                   "budget allocation of two million dollars across engineering and operations teams.",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Justify,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "Key stakeholders include the VP of Engineering, the CTO, and department leads " +
                   "from Product, Security, and Operations. A steering committee will convene monthly " +
                   "to review progress against milestones and address any blockers or scope changes.",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Justify
        });

        doc.Add(new TextBlock
        {
            Text = "Section 2: Expected Outcomes",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = ColorPalette.Blue[800],
            Margin = new Thickness(0, 18, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "On completion, the platform is expected to reduce infrastructure costs by twenty " +
                   "percent, cut average deployment time from hours to minutes, and improve service " +
                   "availability to four nines. These outcomes will be measured against a baseline " +
                   "captured during the discovery phase and reported to the steering committee.",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Justify
        });

        return doc;
    }
}
