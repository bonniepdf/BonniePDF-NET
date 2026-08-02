using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.ProductPhotoGallery;

/// <summary>
/// A retail product-photo gallery / look-book. Products are laid out two-per-row
/// as photo cards with name, SKU, price, and blurb in an accessible PDF/A-2a +
/// PDF/UA-1 document. Product photographs are represented by labelled
/// placeholders until real assets are supplied.
/// </summary>
[System.ComponentModel.DisplayName("Product Photo Gallery")]
[System.ComponentModel.Category("Catalogs")]
[System.ComponentModel.Description("A retail look-book: products laid out two-per-row as photo cards with name, SKU, price, and blurb, paginating automatically. PDF/A-2a + PDF/UA-1.")]
public static class ProductPhotoGalleryExample
{
    private const float CardImageHeight = 128f;

    private static readonly Color Accent = ColorPalette.Emerald[700];
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Body = ColorPalette.Slate[600];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Product[] Products =
    [
        new("Terracotta Planter", "HG-1042", "GBP 28.00", "Hand-thrown stoneware planter with a matte terracotta glaze."),
        new("Linen Throw", "HG-2277", "GBP 54.00", "Stonewashed pure-linen throw in oatmeal, finished with a hand-knotted fringe."),
        new("Brass Reading Lamp", "LT-0391", "GBP 120.00", "Adjustable desk lamp in brushed brass with a weighted base and warm-dim LED."),
        new("Ceramic Pour-Over", "KT-5108", "GBP 36.00", "Single-cup pour-over dripper in speckled clay, sized for a standard 02 filter."),
        new("Wool Floor Cushion", "HG-3315", "GBP 95.00", "Chunky hand-woven wool floor cushion with a removable, washable cover."),
        new("Smoked-Glass Carafe", "KT-4420", "GBP 42.00", "Mouth-blown smoked-glass carafe and tumbler set for the bedside or table."),
        new("Oak Wall Shelf", "FN-0876", "GBP 68.00", "Solid white-oak floating shelf with concealed brackets and an oiled finish."),
        new("Rattan Pendant Shade", "LT-1653", "GBP 82.00", "Open-weave rattan pendant shade that casts a soft, patterned light."),
    ];

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
                    TextColor = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                };
                return footer;
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[700]);

        Document doc = new(settings);
        doc.Metadata.Title = "Product Gallery - Spring Collection";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "An accessible PDF/A-2a + PDF/UA-1 retail product-photo gallery.";
        doc.Metadata.Keywords = "pdf, accessibility, retail, catalogue, product gallery, e-commerce, BonniePDF";

        Heading h1 = new("Spring Collection")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Ink,
            Margin = new Thickness(0, 0, 0, 4),
        };
        doc.Add(h1);

        doc.Add(new TextBlock
        {
            Text = "Homeware & lighting - selected pieces from the new season.",
            FontSize = 12,
            TextColor = Muted,
            Margin = new Thickness(0, 0, 0, 18),
        });

        VerticalStack gallery = new() { Spacing = 16 };
        for (int i = 0; i < Products.Length; i += 2)
        {
            HorizontalStack row = new()
            {
                Spacing = 16,
                WidthDistribution = WidthDistribution.Equal,
            };
            row.Add(ProductCard(Products[i]));
            if (i + 1 < Products.Length) row.Add(ProductCard(Products[i + 1]));
            gallery.Add(row);
        }

        doc.Add(gallery);
        return doc;
    }

    private static Container ProductCard(Product product)
    {
        VerticalStack body = new() { Spacing = 4 };
        body.Add(ProductMedia(product));

        Heading name = new(product.Name)
        {
            TocIdentifier = product.Sku.ToLowerInvariant(),
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            TextColor = Ink,
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
            TextColor = Muted,
        }, 1f);
        meta.Add(new TextBlock
        {
            Text = product.Price,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            TextColor = Accent,
            Alignment = TextAlignment.Right,
        }, 1f);
        body.Add(meta);

        body.Add(new TextBlock
        {
            Text = product.Blurb,
            FontSize = 9.5f,
            TextColor = Body,
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

    private static Container ProductMedia(Product product)
    {
        TextBlock label = new()
        {
            Text = product.Name,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 10,
            TextColor = ColorPalette.Slate[400],
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, CardImageHeight / 2 - 14, 0, 0),
        };

        return new Container
        {
            Height = CardImageHeight,
            BackgroundColor = ColorPalette.Slate[100],
            CornerRadius = 6,
            Child = label,
        };
    }

    private sealed record Product(string Name, string Sku, string Price, string Blurb);
}
