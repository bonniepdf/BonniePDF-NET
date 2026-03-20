using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Hyperlinks;

/// <summary>
/// Demonstrates hyperlink support (internal and external clickable links) using the Fluent API.
/// Hyperlink elements are added via the Element() escape hatch because there is no dedicated
/// fluent terminal method for them — all other styling uses chained extension methods.
/// </summary>
[System.ComponentModel.DisplayName("Hyperlinks (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Internal and external clickable links using the Fluent API.")]
public static class HyperlinksExampleFluent
{
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title   = "Hyperlink Examples (Fluent API)";
                metadata.Author  = "BonniePDF";
                metadata.Subject = "Demonstrating hyperlink support in PDFs using the Fluent API";
            });

            doc.WithSettings(settings =>
            {
                settings.Version        = PdfVersion.Pdf17;
                settings.PdfAEnabled    = true;
                settings.DefaultZoom    = ZoomLevel.FitPage;
                settings.DisplayDocTitle = true;
            });

            // Document-wide default: Helvetica 12pt
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12));

            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    // ── Title ─────────────────────────────────────────────────────
                    col.Item()
                        .Text("Hyperlink Support Demo (Fluent API)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    // ── Basic Links ────────────────────────────────────────────────
                    SectionHeader(col, "Basic Links:");

                    col.Item().Element(new Hyperlink("https://github.com",    "Visit GitHub")      { Margin = new Thickness(0, 0, 0, 8) });
                    col.Item().Element(new Hyperlink("https://microsoft.com", "Visit Microsoft")   { Margin = new Thickness(0, 0, 0, 8) });
                    col.Item().Element(new Hyperlink("https://google.com",    "Search on Google")  { Margin = new Thickness(0, 0, 0, 16) });

                    // ── Styled Links ───────────────────────────────────────────────
                    SectionHeader(col, "Styled Links:");

                    // Default blue
                    col.Item().Element(new Hyperlink("https://github.com", "Default Blue Link")
                    {
                        Margin = new Thickness(0, 0, 0, 8)
                    });

                    // Green
                    col.Item().Element(new Hyperlink("https://microsoft.com", "Green Link")
                    {
                        TextColor = Color.Green500,
                        Margin    = new Thickness(0, 0, 0, 8)
                    });

                    // Large purple
                    col.Item().Element(new Hyperlink("https://google.com", "Large Purple Link")
                    {
                        FontSize  = 16,
                        TextColor = Color.Purple500,
                        Margin    = new Thickness(0, 0, 0, 8)
                    });

                    // Bold red
                    col.Item().Element(new Hyperlink("https://stackoverflow.com", "Bold Red Link")
                    {
                        FontName  = StandardFonts.HelveticaBold,
                        FontSize  = 14,
                        TextColor = Color.Red,
                        Margin    = new Thickness(0, 0, 0, 16)
                    });

                    // ── Links with Margins ─────────────────────────────────────────
                    SectionHeader(col, "Links with Margins:");

                    col.Item()
                        .Text("Text before link.")
                        .FontSize(10);

                    col.Item().Element(new Hyperlink("https://github.com", "Link with 20pt vertical margins")
                    {
                        Margin = new Thickness(0, 20, 0, 20)
                    });

                    col.Item()
                        .Text("Text after link.")
                        .FontSize(10);
                });
            });

        }).Build();

    }

    /// <summary>Adds a bold Slate700 section header at 14pt.</summary>
    private static void SectionHeader(Fluent.Descriptors.ColumnDescriptor col, string title)
    {
        col.Item()
            .Text(title)
            .Font(StandardFonts.HelveticaBold)
            .FontSize(14)
            .TextColor(Color.Slate700)
            .Margin(0, 0, 0, 8);
    }
}

