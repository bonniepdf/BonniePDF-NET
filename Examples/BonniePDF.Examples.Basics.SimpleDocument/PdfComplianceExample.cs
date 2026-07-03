using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>
/// PDF/A-2a + PDF/UA-1 - the recommended modern baseline.
///
/// <para>
/// PDF/A-2a is the strongest of the "basic + accessible" archival levels:
/// PDF/A-2 brings modern PDF 1.7 visual fidelity (transparency, JPEG2000,
/// OpenType font embedding) on top of PDF/A-1, and the "a" suffix adds
/// tagged structure - i.e. it presupposes PDF/UA-1.
/// </para>
///
/// <para>
/// Setting <see cref="DocumentSettings.Conformance"/> auto-upgrades
/// <see cref="DocumentSettings.Version"/> to PDF 1.7 and emits the
/// <c>pdfaid:part 2 / pdfaid:conformance A</c> XMP claim, the sRGB ICC
/// profile, the <c>/OutputIntents</c> entry, and the
/// <c>pdfaExtension:schemas</c> declaration for the <c>pdfuaid</c>
/// namespace (PDF/A-2 §6.6.2.3.1). The "a" level requires
/// <c>Accessibility = AccessibilityMode.PdfUa1</c> - the document
/// throws at save time otherwise.
/// </para>
///
/// Verified against veraPDF profile <c>2a</c>.
/// </summary>
[System.ComponentModel.DisplayName("PDF/A + PDF/UA")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("The modern archival + accessible baseline. Uses Conformance = PdfA2a alongside PdfUa1.")]
public static class PdfComplianceExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "PDF/A-2a Compliant Document",
                Author = "BonniePDF",
                Subject = "A minimal PDF/A-2a + PDF/UA-1 document - archival and accessible.",
                Keywords = "pdf, pdf/a-2a, pdf/ua, accessibility, archival, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                // Conformance is the source of truth. PdfA2a auto-upgrades
                // Version to PDF 1.7 and triggers all the PDF/A emission machinery.
                Conformance = PdfAConformance.PdfA2a,
                // "a" levels require PDF/UA-1 tagging.
                Accessibility = AccessibilityMode.PdfUa1,
                // BCP-47 tag - required for PDF/UA-1.
                Language = "en-GB",
            },
        };

        doc.Add(new Heading("PDF/A-2a + PDF/UA-1")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "This document targets PDF/A-2a - the \"accessible archival\" profile from ISO 19005-2. " +
                   "It is simultaneously a long-term archival file (PDF/A) and a screen-reader-friendly document (PDF/UA-1). " +
                   "Both claims are verified by veraPDF against profiles 2a and ua1 respectively.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new Heading("Why PDF/A-2a over PDF/A-1b?")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 16, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "PDF/A-1b (2005) is based on PDF 1.4 and disallows transparency, JPEG2000, and modern font features. " +
                   "PDF/A-2a (2011) is based on PDF 1.7, lifts those restrictions, and crucially requires the same tagged " +
                   "structure that the European Accessibility Act (EAA) calls for. For new documents authored in 2026 onward, " +
                   "PDF/A-2a is the natural target - and BonniePDF reaches it with two lines of configuration.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "The legacy boolean Settings.PdfAEnabled = true still works and maps to PDF/A-2b under the hood - " +
                   "the safe automatic upgrade from the historic PDF/A-1b baseline. For new code, prefer the explicit " +
                   "Settings.Conformance = PdfAConformance.PdfA2a shown above.",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = ColorPalette.Slate[600],
        });

        return doc;
    }
}
