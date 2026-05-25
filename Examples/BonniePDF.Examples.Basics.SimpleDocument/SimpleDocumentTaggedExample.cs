using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>
/// The this feature stage-0 closeout example. Same shape as
/// <see cref="SimpleDocumentExample"/> but with PDF/UA-1 tagging
/// enabled: <c>Accessibility = PdfUa1</c>, <c>Language = "en-GB"</c>,
/// a tagged <see cref="Heading"/>, paragraphs that pick up implicit
/// <c>P</c> tagging from <see cref="TextBlock"/>, and a
/// <see cref="QRCode"/> with alt-text producing a <c>Figure</c>
/// structure element.
///
/// Acceptance: VeraPDF profile <c>ua1</c> reports zero failures on
/// the generated PDF. See
/// <c>BonniePDF.Tests/VeraPdfStageZeroCloseoutTests.cs</c>.
/// </summary>
[System.ComponentModel.DisplayName("Simple Document (Tagged / PDF-UA-1)")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("PDF/UA-1 tagged variant of SimpleDocumentExample — this feature stage-0 closeout.")]
public static class SimpleDocumentTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Simple Document — Tagged (PDF/UA-1)",
                Author = "BonniePDF",
                Subject = "this feature stage-0 closeout: a screen-reader-friendly tagged PDF.",
                Keywords = "pdf, pdf/ua, accessibility, tagged, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        // H1 — implicit via Heading + Level
        doc.Add(new Heading("Accessible PDF Demo")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12),
        });

        // Two paragraphs — implicit P tagging via TextBlock
        doc.Add(new TextBlock
        {
            Text = "This document opts into PDF/UA-1 tagging by setting Settings.Accessibility = AccessibilityMode.PdfUa1. " +
                   "The writer emits a structure tree, marked-content operators around every visible text run, " +
                   "and an XMP packet declaring pdfuaid:part 1.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "Existing elements (Heading, TextBlock, Image, QRCode) tag themselves automatically — no semantic " +
                   "verbs required. Authors who want explicit control over the structure tree can use the Semantic* " +
                   "extensions that arrive in stage 2.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 12),
        });

        // H2 — sub-heading
        doc.Add(new Heading("Scan to verify")
        {
            TocIdentifier = "qr",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Figure with alt-text — implicit Figure + /Alt
        doc.Add(new QRCode
        {
            Data = "https://github.com/bonniepdf/BonniePDF-NET",
            Size = 100,
            AltText = "QR code linking to the BonniePDF GitHub repository",
        });

        return doc;
    }
}
