using BonniePDF;
namespace BonniePDF.Examples.ImagesSvgColour.Images;

/// <summary>Fit modes, transparency and alt text using explicit PNG assets.</summary>
public static class ImagesExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings { Language = "en-GB", Conformance = PdfAConformance.PdfA2a, Accessibility = AccessibilityMode.PdfUa1, Margin = 40 }) { Metadata = new DocumentMetadata { Title = "Images" } };
        doc.Add(new Heading("Images") { Level = 1, FontSize = 24 });
        foreach (var mode in new[] { ImageFitMode.Contain, ImageFitMode.Cover, ImageFitMode.Fill })
        {
            string name = mode.ToString().ToLowerInvariant();
            doc.Add(new Heading(name) { Level = 2, FontSize = 14 });
            doc.Add(new Image { ImageData = Asset("gradient.png"), Width = 240, Height = 100, FitMode = mode, AltText = $"Colour gradient with {name} fitting." });
        }
        doc.Add(new Heading("Transparency") { Level = 2, FontSize = 14 });
        doc.Add(new Container { BackgroundColor = ColorPalette.Blue[100], Padding = new Thickness(10), Child = new Image { ImageData = Asset("alpha.png"), Width = 100, Height = 100, AltText = "A transparent colour gradient over a blue background." } });
        doc.Add(new TextBlock("Informative images need alt text; decorative images are skipped by screen readers."));
        doc.Add(new Image { ImageData = Asset("divider.png"), Width = 240, Height = 8, IsDecorative = true });
        return doc;
    }
    private static byte[] Asset(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "images", "recipes", name));
}
