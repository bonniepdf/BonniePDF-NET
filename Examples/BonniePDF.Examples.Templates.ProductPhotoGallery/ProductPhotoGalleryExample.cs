using BonniePDF;

namespace BonniePDF.Examples.Templates.ProductPhotoGallery;

/// <summary>
/// A retail product gallery / look-book. Products are laid out two-per-row as
/// cards with name, SKU, price, and blurb in an accessible PDF/A-2a + PDF/UA-1
/// document that paginates automatically. Each card leads with a deterministic
/// colour-blocked tile - a per-product tint with the product's initial - drawn
/// natively by the engine, so the catalogue is self-contained and needs no
/// bundled photography.
/// </summary>
[System.ComponentModel.DisplayName("Product Gallery")]
[System.ComponentModel.Category("Retail")]
[System.ComponentModel.Description(
    "A retail look-book: products laid out two-per-row as cards with name, SKU, price, and blurb, paginating automatically. Each card leads with a deterministic colour-blocked tile drawn natively by the engine, so the catalogue is self-contained. PDF/A-2a + PDF/UA-1.")]
public static class ProductPhotoGalleryExample
{
    private const float CardImageHeight = 128f;

    private static readonly Color Accent = ColorPalette.Emerald[700];
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Body = ColorPalette.Slate[600];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];

    // One tint per card, cycled by position, so the catalogue reads as a
    // deliberate colour-blocked design rather than a set of missing images.
    private static readonly ColorPalette[] TileHues =
    [
        ColorPalette.Emerald,
        ColorPalette.Amber,
        ColorPalette.Rose,
        ColorPalette.Sky,
        ColorPalette.Violet,
        ColorPalette.Teal,
        ColorPalette.Orange,
        ColorPalette.Indigo,
    ];

    private static readonly Product[] Products =
    [
        new("Terracotta Planter", "HG-1042", "£28.00",
            "Hand-thrown stoneware planter with a matte terracotta glaze."),
        new("Linen Throw", "HG-2277", "£54.00",
            "Stonewashed pure-linen throw in oatmeal, finished with a hand-knotted fringe."),
        new("Brass Reading Lamp", "LT-0391", "£120.00",
            "Adjustable desk lamp in brushed brass with a weighted base and warm-dim LED."),
        new("Ceramic Pour-Over", "KT-5108", "£36.00",
            "Single-cup pour-over dripper in speckled clay, sized for a standard 02 filter."),
        new("Wool Floor Cushion", "HG-3315", "£95.00",
            "Chunky hand-woven wool floor cushion with a removable, washable cover."),
        new("Smoked-Glass Carafe", "KT-4420", "£42.00",
            "Mouth-blown smoked-glass carafe and tumbler set for the bedside or table."),
        new("Oak Wall Shelf", "FN-0876", "£68.00",
            "Solid white-oak floating shelf with concealed brackets and an oiled finish."),
        new("Rattan Pendant Shade", "LT-1653", "£82.00",
            "Open-weave rattan pendant shade that casts a soft, patterned light."),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber =>
            {
                TextBlock footer = new()
                {
                    Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                    FontName = StandardFonts.Helvetica,
                    FontSize = 9,
                    Color = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                };
                return footer;
            },
        };
        settings.DefaultTextStyle =
            new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[700]);

        Document doc = new(settings);
        doc.Metadata.Title = "Product Gallery - Spring Collection";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "An accessible PDF/A-2a + PDF/UA-1 retail product gallery.";
        doc.Metadata.Keywords = "pdf, accessibility, retail, catalogue, product gallery, e-commerce, BonniePDF";

        Heading h1 = new("Spring Collection")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            Color = Ink,
            Margin = new Thickness(0, 0, 0, 4),
        };
        doc.Add(h1);

        doc.Add(new TextBlock
        {
            Text = "Homeware & lighting - selected pieces from the new season.",
            FontSize = 12,
            Color = Muted,
            Margin = new Thickness(0, 0, 0, 18),
        });

        doc.Add(BuildGallery());
        return doc;
    }

    private static VerticalStack BuildGallery()
    {
        VerticalStack gallery = new() { Spacing = 16 };
        for (int i = 0; i < Products.Length; i += 2)
        {
            HorizontalStack row = new()
            {
                Spacing = 16,
                WidthDistribution = WidthDistribution.Equal,
            };
            row.Add(ProductCard(Products[i], i));
            if (i + 1 < Products.Length) row.Add(ProductCard(Products[i + 1], i + 1));
            gallery.Add(row);
        }

        return gallery;
    }

    private static Container ProductCard(Product product, int index)
    {
        VerticalStack body = new() { Spacing = 4 };
        body.Add(ProductMedia(product, index));

        Heading name = new(product.Name)
        {
            TocIdentifier = product.Sku.ToLowerInvariant(),
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            Color = Ink,
            Margin = new Thickness(0, 6, 0, 0),
        };
        body.Add(name);

        HorizontalStack meta = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
        };
        meta.Add(new TextBlock
        {
            Text = product.Sku,
            FontSize = 9,
            Color = Muted,
        }, 1f);
        meta.Add(new TextBlock
        {
            Text = product.Price,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            Color = Accent,
            Alignment = TextAlignment.Right,
        }, 1f);
        body.Add(meta);

        body.Add(new TextBlock
        {
            Text = product.Blurb,
            FontSize = 9.5f,
            Color = Body,
        });

        return new Container
        {
            Child = body,
            Padding = new Thickness(12),
            BackgroundColor = Color.White,
            CornerRadius = 8,
            Border = new Border(1),
            BorderColors = new BorderColors(Rule),
        };
    }

    private static Container ProductMedia(Product product, int index)
    {
        ColorPalette hue = TileHues[index % TileHues.Length];

        // Amber[600] is the one shade in this cycle that falls short of 3:1 on its
        // own [100] tint (2.87:1); every other hue already clears it at [600].
        Color monogramColor = hue == ColorPalette.Amber ? ColorPalette.Amber[700] : hue[600];

        TextBlock monogram = new()
        {
            Text = product.Name[..1].ToUpperInvariant(),
            FontName = StandardFonts.HelveticaBold,
            FontSize = 40,
            Color = monogramColor,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 40, 0, 0),
        };

        return new Container
        {
            Height = CardImageHeight,
            BackgroundColor = hue[100],
            CornerRadius = 6,
            Child = monogram,
        };
    }

    private sealed record Product(string Name, string Sku, string Price, string Blurb);
}
