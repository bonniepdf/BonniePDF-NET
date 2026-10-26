using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Build from explicit bytes; the host writes a file or returns a PDF response.</summary>
public static class BrowserDownloadExample
{
    public static Document Generate()
    {
        byte[] font = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSans-Regular.ttf"));
        byte[] photo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "images", "products", "terracotta-planter.jpg"));
        return Build(font, photo);
    }

    public static Document Build(byte[] font, byte[] photo)
    {
        var doc = new Document(new DocumentSettings { Language = "en-GB", Margin = 40 }) { Metadata = new DocumentMetadata { Title = "Your browser download" } };
        var face = doc.Fonts.Add(font);
        doc.Add(new Heading("Your browser download") { Level = 1, FontName = face.FontName, FontSize = 24 });
        doc.Add(new TextBlock("The font and photograph were supplied as bytes. The browser host awaits generation, checks errors, and downloads the PDF.") { FontName = face.FontName, Margin = new Thickness(0, 0, 0, 18) });
        doc.Add(new Image { ImageData = photo, Width = 200, Height = 160, FitMode = ImageFitMode.Contain, AltText = "A terracotta planter." });
        return doc;
    }
}
