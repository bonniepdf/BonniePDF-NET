using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples;

/// <summary>
/// Demonstrates hyperlink support in PDF documents
/// </summary>
public class HyperlinkExample
{
    public static string Generate(string outputPath = "hyperlink_example.pdf")
    {
        // Create a document with metadata
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set document metadata
        doc.Metadata.Title = "Hyperlink Examples";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrating hyperlink support in PDFs";

        // Configure document settings
        doc.Settings.DefaultZoom = ZoomLevel.FitPage;
        doc.Settings.DisplayDocTitle = true;

        // Title
        doc.Add(new TextBlock
        {
            Text = "Hyperlink Support Demo",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Basic Links Section
        doc.Add(new TextBlock
        {
            Text = "Basic Links:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
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
        doc.Add(new TextBlock
        {
            Text = "Styled Links:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Default blue link
        doc.Add(new Hyperlink("https://github.com", "Default Blue Link")
        {
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Green link
        doc.Add(new Hyperlink("https://microsoft.com", "Green Link")
        {
            TextColor = Color.Green500,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Larger font
        doc.Add(new Hyperlink("https://google.com", "Large Purple Link")
        {
            FontSize = 16,
            TextColor = Color.Purple500,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // Bold link
        doc.Add(new Hyperlink("https://stackoverflow.com", "Bold Red Link")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Red,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Margins Section
        doc.Add(new TextBlock
        {
            Text = "Links with Margins:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700
        });

        doc.Add(new TextBlock { Text = "Text before link.", FontSize = 10 });

        // Link with margins
        doc.Add(new Hyperlink("https://github.com", "Link with 20pt vertical margins")
        {
            Margin = new Thickness(0, 20, 0, 20)
        });

        doc.Add(new TextBlock { Text = "Text after link.", FontSize = 10 });

        // Save the document
        doc.Save(outputPath);

        return outputPath;
    }
}