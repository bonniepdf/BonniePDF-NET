using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.ShelfLabel;

/// <summary>
/// EU/UK retail shelf-edge label on a custom 110 x 75 mm card, with EAN-13,
/// promotional was/now pricing, mandatory unit price, shelf metadata, and
/// accessibility/PDF-A settings.
/// </summary>
[System.ComponentModel.DisplayName("Retail Shelf Label (EU/UK)")]
[System.ComponentModel.Category("Retail")]
[System.ComponentModel.Description("EU/UK retail shelf-edge label on a custom 110x75mm card: product details, promotional pricing, mandatory unit price, shelf metadata, and EAN-13 barcode. PDF/A-2a + PDF/UA-1.")]
public static class ShelfLabelExample
{
    private const float DetailTileHeight = 31;

    private static readonly Color Accent = ColorPalette.Emerald[700];
    private static readonly Color AccentDark = ColorPalette.Emerald[900];
    private static readonly Color AccentLight = ColorPalette.Emerald[50];
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color Body = ColorPalette.Slate[600];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Rule = ColorPalette.Slate[200];
    private static readonly Color Promo = ColorPalette.Rose[600];

    public static Document Generate()
    {
        DocumentSettings settings = new(new PdfSize(311.8f, 212.6f, "Shelf Label 110x75"), 8)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        Document doc = new(settings);
        doc.Metadata.Title = "Harvest Grove Organic Rolled Oats - shelf label";
        doc.Metadata.Author = "Greenfields";
        doc.Metadata.Subject = "EU/UK retail shelf-edge label with EAN-13 and mandatory unit price.";

        VerticalStack card = new();
        card.AddChild(RetailerStrip());
        card.AddChild(MainOffer());
        card.AddChild(DetailRow());
        card.AddChild(BarcodePanel());

        doc.Add(new Container
        {
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            Padding = new Thickness(0),
            Child = card,
        });

        return doc;
    }

    private static Container RetailerStrip()
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Alignment = HorizontalAlignment.Center };
        row.AddChild(Tb("GREENFIELDS", 10, Color.White, bold: true), 1);
        row.AddChild(Tb("Everyday Low Price", 9.4f, Color.White, bold: true, align: TextAlignment.Right), 1);

        return new Container
        {
            BackgroundColor = Accent,
            Padding = new Thickness(9, 4, 9, 4),
            Child = row,
        };
    }

    private static Grid MainOffer()
    {
        VerticalStack info = new();
        info.AddChild(Tb("HARVEST GROVE", 8, AccentDark, bold: true, bottom: 2));
        info.AddChild(new Heading("Organic Rolled Oats")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Ink,
            Margin = new Thickness(0, 0, 0, 1),
        });
        info.AddChild(Tb("1 kg  -  Milled in the UK", 9, Body));

        VerticalStack price = new();
        price.AddChild(Tb("Was " + Money(2.99), 8.5f, Promo, bold: true, align: TextAlignment.Right));
        price.AddChild(Tb(Money(2.49), 30, Ink, bold: true, align: TextAlignment.Right));
        price.AddChild(UnitPriceBadge());

        Grid row = new(1.75f, 1)
        {
            Border = Border.None,
            CellPadding = 8,
        };
        row.AddRow(new Container { Child = info, Padding = new Thickness(0, 0, 6, 0) }, price);
        return row;
    }

    private static Container UnitPriceBadge() => new()
    {
        BackgroundColor = AccentLight,
        Border = new Border(0.5f),
        BorderColors = new BorderColors(ColorPalette.Emerald[200]),
        Padding = new Thickness(5, 2, 5, 2),
        Margin = new Thickness(0, 1, 0, 0),
        Child = Tb(Money(0.25) + "/100 g", 8.2f, AccentDark, bold: true, align: TextAlignment.Right),
    };

    private static Grid DetailRow()
    {
        Grid table = new(1, 1, 1)
        {
            Border = Border.None,
            CellPadding = 0,
            Margin = new Thickness(8, 0, 8, 6),
        };
        table.AddRow(
            DetailCell("SAVE", Money(0.50), Promo),
            DetailCell("SKU", "HG-OAT-1KG", Body),
            DetailCell("VAT 0%", "Aisle 3B / Shelf 2", Body));
        return table;
    }

    private static Container DetailCell(string label, string value, Color accent)
    {
        VerticalStack stack = new();
        stack.AddChild(Tb(label, 5.8f, Muted, bold: true, align: TextAlignment.Center));
        stack.AddChild(Tb(value, 7.2f, accent, bold: true, align: TextAlignment.Center));

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Height = DetailTileHeight,
            Padding = new Thickness(4, 3, 4, 3),
            Margin = new Thickness(1, 0, 1, 0),
            Child = stack,
        };
    }

    private static Container BarcodePanel()
    {
        VerticalStack stack = new();
        stack.AddChild(Tb("EAN-13 / GS1 UK article number", 6.5f, Muted, bold: true, align: TextAlignment.Center, bottom: 2));
        stack.AddChild(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Barcode
            {
                Value = "501234567890",
                Symbology = BarcodeSymbology.Ean13,
                Height = 58,
                AltText = "EAN-13 article number 5012345678900.",
            },
        });
        stack.AddChild(Tb("OATS  -  BEST BEFORE SEE PACK  -  STORE COOL & DRY", 6.2f, Body, bold: true, align: TextAlignment.Center, bottom: 1));
        stack.AddChild(Tb("SEL v04  -  Price file GF-27  -  Promo ends 30 Jul 2026", 5.8f, Muted, align: TextAlignment.Center));

        return new Container
        {
            Border = new Border(0, 0.75f, 0, 0),
            BorderColors = new BorderColors(null, Rule, null, null),
            Padding = new Thickness(8, 9, 8, 12),
            Child = stack,
        };
    }

    private static TextBlock Tb(string content, float size, Color color, bool bold = false,
        TextAlignment? align = null, float bottom = 0)
    {
        TextBlock block = new()
        {
            Text = content,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color,
        };
        if (align.HasValue) block.Alignment = align.Value;
        if (bottom > 0) block.Margin = new Thickness(0, 0, 0, bottom);
        return block;
    }

    private static string Money(double amount) => "\u00A3" + amount.ToString("0.00", CultureInfo.InvariantCulture);
}
