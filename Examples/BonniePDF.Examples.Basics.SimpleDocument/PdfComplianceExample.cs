using BonniePDF;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>A concise PDF/A-2a + PDF/UA-1 example.</summary>
[System.ComponentModel.DisplayName("Archival & Accessible PDF")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("A concise PDF/A-2a and PDF/UA-1 example showing settings and responsibilities.")]
public static class PdfComplianceExample
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability", "MA0051:Method is too long",
        Justification = "The example intentionally keeps generation inline.")]
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Archival and Accessible PDF with BonniePDF",
                Author = "BonniePDF",
                Subject = "A concise PDF/A-2a and PDF/UA-1 example.",
                Keywords = "pdf/a-2a, pdf/ua-1, accessibility, archival, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        doc.Add(new Heading("Archival and accessible PDF")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 8),
        });
        doc.Add(new TextBlock
        {
            Text = "BonniePDF treats archival conformance and accessibility as separate settings, so a document can use either standard or combine both.",
            FontSize = 11,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 12),
        });
        doc.Add(new Line
        {
            Thickness = 2,
            Color = ColorPalette.Blue[600],
            Margin = new Thickness(0, 0, 0, 14),
        });

        doc.Add(new Heading("Settings used by this document")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 5),
        });
        UnorderedList settings = new()
        {
            FontSize = 10,
            Color = ColorPalette.Slate[700],
            BulletColor = ColorPalette.Blue[600],
            ItemSpacing = 5,
            Margin = new Thickness(0, 0, 0, 14),
        };
        settings.AddItem("Conformance = PdfA2a - requests modern PDF/A archival output.");
        settings.AddItem("Accessibility = PdfUa1 - emits tagged structure for assistive technology.");
        settings.AddItem("Language = en-GB - declares the document language for screen readers.");
        doc.Add(settings);

        doc.Add(new Heading("Responsibilities")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 5),
        });
        doc.Add(new TextBlock
        {
            Text = "BonniePDF handles the PDF version, conformance metadata, colour profiles, embedded standard-font substitutes, semantic tags, setting validation, and honest-claim resource checks.",
            FontSize = 10,
            Color = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 8),
        });
        doc.Add(new TextBlock
        {
            Text = "The author supplies the required profile, title and language, logical reading order, alt text and table headers, and validates the final PDF against the required external profile.",
            FontSize = 10,
            Color = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 16),
        });

        doc.Add(new Line
        {
            Color = ColorPalette.Slate[300],
            Margin = new Thickness(0, 0, 0, 10),
        });
        doc.Add(new TextBlock
        {
            Text = "Legacy compatibility: PdfAEnabled reads true for any PDF/A profile. Assigning true selects PdfA2b; assigning false selects None. New code should use Conformance.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 9,
            Color = ColorPalette.Slate[500],
        });

        return doc;
    }
}
