using BonniePDF;

namespace BonniePDF.Examples.LinksReferences.Hyperlinks;

/// <summary>
/// Demonstrates hyperlink support in PDF documents
/// </summary>
[System.ComponentModel.DisplayName("Hyperlinks")]
[System.ComponentModel.Category("Links & References")]
[System.ComponentModel.Description("Internal and external clickable links.")]
public static class HyperlinksExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        // Create a document with metadata
        Document doc = new();

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle =
            new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        doc.Metadata.Title = "Hyperlink Examples";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating hyperlink support in PDFs";

        // Configure document settings
        doc.Settings.DefaultZoom = ZoomLevel.FitPage;
        doc.Settings.DisplayDocTitle = true;

        // Title — a real Heading (H1), not a big TextBlock: PDF/UA preflight
        // requires the outline to start at H1, and headings give AT users
        // document structure.
        doc.Add(new Heading
        {
            Text = "Hyperlink Support Demo",
            TocIdentifier = "title",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Basic Links Section
        doc.Add(new Heading
        {
            Text = "Basic Links",
            TocIdentifier = "basic-links",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Add(new Hyperlink("https://github.com", "Visit GitHub")
        {
            Margin = new Thickness(0, 0, 0, 8)
        });
        doc.Add(new Hyperlink("https://microsoft.com", "Visit Microsoft")
        {
            Margin = new Thickness(0, 0, 0, 8)
        });
        doc.Add(new Hyperlink("https://google.com", "Search on Google")
        {
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Styled Links Section
        doc.Add(new Heading
        {
            Text = "Styled Links",
            TocIdentifier = "styled-links",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Default blue link
        doc.Add(new Hyperlink("https://github.com", "Default Blue Link")
        {
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Green link. Green[700], not [500]: Hyperlink exposes no bold/weight
        // property for the compliance preflight to detect, so this always needs
        // the regular-text 4.5:1 threshold regardless of FontName; [500] only
        // reaches 2.22:1.
        doc.Add(new Hyperlink("https://microsoft.com", "Green Link")
        {
            Color = ColorPalette.Green[700],
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Larger font. Purple[600], not [500]: 16pt isn't large enough to relax the
        // threshold (needs 18pt regular or 14pt bold), and [500] only reaches
        // 4.12:1.
        doc.Add(new Hyperlink("https://google.com", "Large Purple Link")
        {
            FontSize = 16,
            Color = ColorPalette.Purple[600],
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Bold link. Red[700], not the named Color.Red: FontName alone doesn't
        // register as bold for Hyperlink (see above), so this needs 4.5:1 too, and
        // pure red only reaches 4.00:1.
        doc.Add(new Hyperlink("https://stackoverflow.com", "Bold Red Link")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Red[700],
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Margins Section
        doc.Add(new Heading
        {
            Text = "Links with Margins",
            TocIdentifier = "links-with-margins",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = ColorPalette.Slate[600]
        });

        doc.Add(new TextBlock { Text = "Text before link.", FontSize = 10 });

        // Link with margins
        doc.Add(new Hyperlink("https://github.com", "Link with 20pt vertical margins")
        {
            Margin = new Thickness(0, 20, 0, 20)
        });

        doc.Add(new TextBlock { Text = "Text after link.", FontSize = 10 });

        // Save the document
        return doc;
    }
}
