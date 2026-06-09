using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Security.Watermarking;

/// <summary>
/// Demonstrates a "DRAFT" watermark rendered behind page content.
/// Watermarks use PDF ExtGState for opacity and the cm operator for rotation,
/// appearing as semi-transparent diagonal text behind all page elements.
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
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(Color.Slate700);

        doc.Metadata.Title = "Draft Document with Watermark";
        doc.Metadata.Author = "BonniePDF";

        // Apply the watermark - default settings: gray, 15% opacity, -45 degrees, 72pt
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
        return doc;
    }
}

