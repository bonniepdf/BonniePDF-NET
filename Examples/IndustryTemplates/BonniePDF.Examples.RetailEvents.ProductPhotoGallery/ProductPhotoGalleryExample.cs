using System.Linq;
using BonniePDF;

namespace BonniePDF.Examples.RetailEvents.ProductPhotoGallery;

/// <summary>
/// A retail product gallery / look-book. Products are laid out two-per-row as
/// cards with studio photography, name, SKU, price, and blurb in an accessible
/// PDF/A-2a + PDF/UA-1 document that paginates automatically. Each card leads
/// with a rounded, clipped product photograph tagged as a Figure with alt text.
/// </summary>
[System.ComponentModel.DisplayName("Product Gallery")]
[System.ComponentModel.Category("Retail & Events")]
[System.ComponentModel.Description(
    "A retail look-book: products laid out two-per-row as cards with studio photography, name, SKU, price, and blurb, paginating automatically. Each photograph is clipped to a rounded frame and carries alt text. PDF/A-2a + PDF/UA-1.")]
public static class ProductPhotoGalleryExample
{
    private const float CardImageHeight = 128f;
    private const float CardImageRadius = 6f;

    private static readonly Color Accent = ColorPalette.Emerald[700];
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Body = ColorPalette.Slate[600];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];

    private static readonly Product[] Products =
    [
        new("Terracotta Planter", "HG-1042", "£28.00", "terracotta-planter",
            "Hand-thrown stoneware planter with a matte terracotta glaze."),
        new("Linen Throw", "HG-2277", "£54.00", "linen-throw",
            "Stonewashed pure-linen throw in oatmeal, finished with a hand-knotted fringe."),
        new("Brass Reading Lamp", "LT-0391", "£120.00", "brass-reading-lamp",
            "Adjustable desk lamp in brushed brass with a weighted base and warm-dim LED."),
        new("Ceramic Pour-Over", "KT-5108", "£36.00", "ceramic-pour-over",
            "Single-cup pour-over dripper in speckled clay, sized for a standard 02 filter."),
        new("Wool Floor Cushion", "HG-3315", "£95.00", "wool-floor-cushion",
            "Chunky hand-woven wool floor cushion with a removable, washable cover."),
        new("Smoked-Glass Carafe", "KT-4420", "£42.00", "smoked-glass-carafe",
            "Mouth-blown smoked-glass carafe and tumbler set for the bedside or table."),
        new("Oak Wall Shelf", "FN-0876", "£68.00", "oak-wall-shelf",
            "Solid white-oak floating shelf with concealed brackets and an oiled finish."),
        new("Rattan Pendant Shade", "LT-1653", "£82.00", "rattan-pendant-shade",
            "Open-weave rattan pendant shade that casts a soft, patterned light."),
    ];

    /// <summary>The asset paths (relative to <c>Examples/assets/</c>) the example needs pre-loaded.</summary>
    public static IReadOnlyList<string> RequiredAssets { get; } = Products.Select(AssetPath).ToArray();

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

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

        doc.Add(BuildGallery(assets));
        return doc;
    }

    private static VerticalStack BuildGallery(IReadOnlyDictionary<string, byte[]> assets)
    {
        VerticalStack gallery = new() { Spacing = 16 };
        for (int i = 0; i < Products.Length; i += 2)
        {
            HorizontalStack row = new()
            {
                Spacing = 16,
                WidthDistribution = WidthDistribution.Equal,
            };
            row.Add(ProductCard(Products[i], assets));
            if (i + 1 < Products.Length) row.Add(ProductCard(Products[i + 1], assets));
            gallery.Add(row);
        }

        return gallery;
    }

    private static Container ProductCard(Product product, IReadOnlyDictionary<string, byte[]> assets)
    {
        VerticalStack body = new() { Spacing = 4 };
        body.Add(ProductMedia(product, assets));

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

    private static string AssetPath(Product product) => $"images/products/{product.ImageSlug}.jpg";

    private static byte[] ImageBytes(Product product, IReadOnlyDictionary<string, byte[]> assets)
    {
        string path = AssetPath(product);
        if (!assets.TryGetValue(path, out byte[]? bytes) || bytes.Length == 0)
            throw new InvalidOperationException($"Missing required asset: {path}");
        return bytes;
    }

    private static Container ProductMedia(Product product, IReadOnlyDictionary<string, byte[]> assets)
    {
        return new Container
        {
            Height = CardImageHeight,
            CornerRadius = CardImageRadius,
            ClipContent = true,
            Child = new Image
            {
                ImageData = ImageBytes(product, assets),
                Height = CardImageHeight,
                FitMode = ImageFitMode.Cover,
                AltText = $"{product.Name}: {product.Blurb}",
            },
        };
    }

    private sealed record Product(string Name, string Sku, string Price, string ImageSlug, string Blurb);
}
