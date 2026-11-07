using BonniePDF;
namespace BonniePDF.Examples.ImagesSvgColour.Images;

/// <summary>Image fit modes, a product photo row, transparency and alt text, using explicit JPEG and PNG assets.</summary>
[System.ComponentModel.DisplayName("Images")]
[System.ComponentModel.Category("Images, SVG & Colour")]
[System.ComponentModel.Description("JPEG and PNG embedding: contain, cover and fill in clipping frames, a product photo row, and alpha transparency on light and dark panels. Informative images carry alt text; the decorative divider is skipped.")]
public static class ImagesExample
{
    private static readonly (ImageFitMode Mode, string Label, string Caption)[] FitModes =
    [
        (ImageFitMode.Contain, "Contain", "The whole photo fits inside; the frame shows around it."),
        (ImageFitMode.Cover, "Cover", "The photo fills the frame; whatever overflows it is cropped."),
        (ImageFitMode.Fill, "Fill", "The photo is stretched to the shape of the frame."),
    ];

    private static readonly (string File, string Name, string Price)[] Products =
    [
        ("linen-throw.jpg", "Linen Throw", "£54.00"),
        ("brass-reading-lamp.jpg", "Brass Reading Lamp", "£120.00"),
        ("smoked-glass-carafe.jpg", "Smoked-Glass Carafe", "£42.00"),
        ("oak-wall-shelf.jpg", "Oak Wall Shelf", "£68.00"),
    ];

    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Margin = 40,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[600])
        })
        { Metadata = new DocumentMetadata { Title = "Images" } };

        doc.Add(new Heading("Images") { Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 24, Color = ColorPalette.Slate[900] });
        doc.Add(new TextBlock("JPEG and PNG files embed as they are, at any size you ask for. The fit mode decides how a photo meets a frame of a different shape.")
        {
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 18)
        });

        // Fit modes: one 1.75:1 photo in three portrait frames. Each frame is a fixed-size,
        // rounded Container that clips (ClipContent needs a corner radius) and centres its
        // content, so contain letterboxes, cover crops, and fill stretches inside the same box.
        doc.Add(SectionHeading("Fit modes"));
        var fitRow = new HorizontalStack { Spacing = 18, WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 22) };
        foreach (var (mode, label, caption) in FitModes)
        {
            var column = new VerticalStack { Spacing = 6 };
            column.Add(new Container
            {
                Width = 150,
                Height = 190,
                CornerRadius = 6,
                ClipContent = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Border = new Border(1),
                BorderColors = new BorderColors(ColorPalette.Stone[300]),
                BackgroundColor = ColorPalette.Stone[100],
                Child = new Image
                {
                    ImageData = Asset("products", "ceramic-pour-over.jpg"),
                    Width = 150,
                    Height = 190,
                    FitMode = mode,
                    AltText = $"A ceramic pour-over coffee dripper, shown with {label.ToLowerInvariant()} fitting."
                }
            });
            column.Add(new TextBlock(label) { FontName = StandardFonts.HelveticaBold, FontSize = 11, Color = ColorPalette.Slate[900] });
            column.Add(new TextBlock(caption));
            fitRow.Add(column);
        }
        doc.Add(fitRow);

        // A product row: the everyday use.
        doc.Add(SectionHeading("Product photos"));
        var productRow = new HorizontalStack { Spacing = 12, WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 22) };
        foreach (var (file, name, price) in Products)
        {
            var card = new VerticalStack { Spacing = 4 };
            card.Add(new Container
            {
                Border = new Border(1),
                BorderColors = new BorderColors(ColorPalette.Stone[300]),
                Child = new Image { ImageData = Asset("products", file), Width = 115, AltText = name }
            });
            card.Add(new TextBlock(name) { FontName = StandardFonts.HelveticaBold, Color = ColorPalette.Slate[900] });
            card.Add(new TextBlock(price) { Color = ColorPalette.Emerald[700] });
            productRow.Add(card);
        }
        doc.Add(productRow);

        // Transparency: one PNG with an alpha channel on two backgrounds.
        doc.Add(SectionHeading("Transparency"));
        var alphaRow = new HorizontalStack { Spacing = 12, WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var (background, tone) in new[] { (ColorPalette.Stone[100], "light"), (ColorPalette.Slate[800], "dark") })
        {
            alphaRow.Add(new Container
            {
                BackgroundColor = background,
                CornerRadius = 8,
                Padding = new Thickness(16),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new Image
                {
                    ImageData = Asset("recipes", "alpha.png"),
                    Width = 100,
                    Height = 100,
                    AltText = $"A transparent colour gradient over a {tone} background."
                }
            });
        }
        doc.Add(alphaRow);
        doc.Add(new TextBlock("The same PNG over a light and a dark panel: its alpha channel lets each background show through."));

        doc.Add(new TextBlock("Informative images need alt text; decorative images, like the rule below, are skipped by screen readers.")
        {
            Margin = new Thickness(0, 22, 0, 8)
        });
        doc.Add(new Image { ImageData = Asset("recipes", "divider.png"), Width = 240, Height = 8, FitMode = ImageFitMode.Fill, IsDecorative = true });
        return doc;
    }

    private static Heading SectionHeading(string text) =>
        new(text) { Level = 2, FontName = StandardFonts.HelveticaBold, FontSize = 14, Color = ColorPalette.Slate[900], Margin = new Thickness(0, 0, 0, 10) };

    private static byte[] Asset(string folder, string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "images", folder, name));
}
