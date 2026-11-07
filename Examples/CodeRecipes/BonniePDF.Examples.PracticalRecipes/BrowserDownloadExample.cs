using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Build from explicit bytes; the host writes a file or returns a PDF response.</summary>
[System.ComponentModel.DisplayName("Download a PDF from a Browser")]
[System.ComponentModel.Category("Getting Started")]
[System.ComponentModel.Description("A product card on an A5 page, built from explicit font and photo bytes, with a runnable browser download companion.")]
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
        var doc = new Document(new DocumentSettings { Language = "en-GB", PageSize = PdfSize.A5, Margin = 32 })
        {
            Metadata = new DocumentMetadata { Title = "Terracotta Planter" }
        };
        // The font and photo arrive as bytes, so the same code runs in a browser, a worker or a server.
        var face = doc.Fonts.Add(font);

        doc.Add(new Image
        {
            ImageData = photo,
            Width = 355,
            Height = 200,
            FitMode = ImageFitMode.Cover,
            AltText = "A terracotta planter holding a young plant on a wooden shelf."
        });
        doc.Add(new TextBlock("SPRING COLLECTION  ·  HG-1042")
        {
            FontName = face.FontName,
            FontSize = 8,
            LetterSpacing = 1.2f,
            Color = ColorPalette.Stone[500],
            Margin = new Thickness(0, 20, 0, 4)
        });
        doc.Add(new Heading("Terracotta Planter") { Level = 1, FontName = face.FontName, FontSize = 28, Color = ColorPalette.Stone[900] });
        doc.Add(new TextBlock("Hand-thrown stoneware with a matte terracotta glaze and a drainage hole. 18 cm across; holds a 15 cm nursery pot.")
        {
            FontName = face.FontName,
            FontSize = 11,
            Color = ColorPalette.Stone[700],
            Margin = new Thickness(0, 6, 0, 14)
        });
        doc.Add(new TextBlock("£28.00") { FontName = face.FontName, FontSize = 22, Color = ColorPalette.Amber[800], Margin = new Thickness(0, 0, 0, 20) });
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Stone[100],
            CornerRadius = 8,
            Padding = new Thickness(12),
            Child = new TextBlock("Built in your browser from the font and photo bytes you supplied, then downloaded. No server was involved.")
            {
                FontName = face.FontName,
                FontSize = 9,
                Color = ColorPalette.Stone[700]
            }
        });
        return doc;
    }
}
