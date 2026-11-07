using System.Globalization;
using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>An 80 mm till receipt on one continuous roll: a long basket, accented names, per-line discounts, a return, and scannable receipt codes.</summary>
[System.ComponentModel.DisplayName("Point-of-Sale Receipt: Long Basket")]
[System.ComponentModel.Category("Retail & Events")]
[System.ComponentModel.Description("An 80 mm till receipt on one continuous roll: a long basket, accented names, per-line discounts, a return, and scannable receipt codes.")]
public static class LongBasketReceiptExample
{
    private const string LogoSvgKey = "images/svg/sample-coffee.svg";

    public static IReadOnlyList<string> RequiredAssets { get; } = [LogoSvgKey];

    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color Mute = ColorPalette.Slate[600];

    // A till receipt is one continuous roll, never separate sheets: an 80 mm page tall enough for the basket.
    private static readonly PdfSize Roll = new(226.77f, 1455, "Receipt roll 80mm");

    public sealed record ReceiptLine(string Name, int Quantity, long UnitPence, long DiscountPence);

    public static Document Generate() => Generate(LoadDefaultAssets());

    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        var lines = Enumerable.Range(0, 35).Select(i => new ReceiptLine(i % 3 == 0 ? $"Café crème, oat milk and freshly baked almond croissant — item {i + 1}" : $"Farmhouse pantry selection {i + 1}", 1, 350 + i * 10, i % 7 == 0 ? 50 : 0)).ToList();
        lines.Add(new ReceiptLine("Returned reusable cup", -1, 500, 0));
        return BuildReceipt(lines, assets);
    }

    public static long CalculateTotal(IReadOnlyList<ReceiptLine> lines)
    {
        long total = 0;
        foreach (var line in lines)
        {
            if (line.Quantity == 0 || line.UnitPence < 0 || line.DiscountPence < 0 || (line.Quantity < 0 && line.DiscountPence != 0) || (line.Quantity > 0 && line.DiscountPence > checked(line.Quantity * line.UnitPence))) throw new ArgumentException("Invalid receipt amount or return discount.");
            total = checked(total + line.Quantity * line.UnitPence - line.DiscountPence);
        }
        return total;
    }

    public static Document BuildReceipt(IReadOnlyList<ReceiptLine> lines, IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        long total = CalculateTotal(lines);
        long discounts = lines.Sum(line => line.DiscountPence);
        // Courier shows the Western European characters itself. A character outside them, such as the ř in
        // Dvořák, comes from Courier's metric-compatible fallback face, and Throw turns any character no
        // font can show into an error instead of a gap on the receipt.
        var doc = new Document(new DocumentSettings
        {
            PageSize = Roll,
            Margin = 16,
            Language = "en-GB",
            MissingGlyphs = MissingGlyphs.Throw,
            DefaultTextStyle = new TextStyle().WithFamily("Courier").WithSize(8.5f).WithColor(Ink),
        })
        { Metadata = new DocumentMetadata { Title = "Long-basket receipt #4022" } };

        byte[]? logoBytes = GetOptionalAsset(assets, LogoSvgKey);
        if (logoBytes is not null)
        {
            doc.Add(new Container
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3),
                Child = new Svg { SvgData = Encoding.UTF8.GetString(logoBytes), Width = 22, Height = 22, IsDecorative = true },
            });
        }
        doc.Add(new Heading("SAMPLE COFFEE")
        {
            Level = 1,
            FontName = StandardFonts.CourierBold,
            FontSize = 13,
            Color = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3),
        });
        doc.Add(Center("123 Harbour Road, Bristol BS1 4RW"));
        doc.Add(Rule());
        doc.Add(Row("Receipt", "#4022", labelWeight: 1, valueWeight: 2));
        doc.Add(Row("Date", "25 Oct 2026 16:42", labelWeight: 1, valueWeight: 2));
        doc.Add(Row("Customer", "Zoë Dvořák", labelWeight: 1, valueWeight: 2));
        doc.Add(Rule());

        foreach (var line in lines)
        {
            doc.Add(Row($"{line.Quantity} x {line.Name}", Money(line.Quantity * line.UnitPence)));
            if (line.DiscountPence != 0) doc.Add(Row("    Discount", Money(-line.DiscountPence), color: Mute));
        }
        if (lines.Count == 0) doc.Add(Row("No purchases", Money(0)));

        doc.Add(Rule());
        doc.Add(Row("Items", lines.Count(line => line.Quantity > 0).ToString(CultureInfo.InvariantCulture)));
        doc.Add(Row("Discounts", Money(-discounts)));
        doc.Add(Row("TOTAL GBP", Money(total), bold: true, size: 11));
        doc.Add(Rule());
        doc.Add(Center("Negative quantities are returns."));
        doc.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 8),
            Child = new QRCode { Data = "https://example.com/receipts/4022", Size = 72, AltText = "Open receipt 4022 online." },
        });
        doc.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Barcode { Value = "4022", Symbology = BarcodeSymbology.Code128, ModuleWidth = 1.5f, Height = 38, AltText = "Receipt number 4022." },
        });
        doc.Add(Center("Thank you for shopping with us.", marginTop: 6));
        return doc;
    }

    private static string Money(long pence) => NumberFormatter.Format(pence / 100m, "F2");

    /// <summary>A centred line of receipt text.</summary>
    private static TextBlock Center(string text, float marginTop = 0) => new(text)
    {
        FontName = StandardFonts.Courier,
        FontSize = 8,
        Color = Mute,
        Alignment = TextAlignment.Center,
        Margin = new Thickness(0, marginTop, 0, 0),
    };

    /// <summary>An item and its amount: the text wraps on the left, the amount hugs the right edge.</summary>
    private static HorizontalStack Row(string label, string value, bool bold = false, float size = 8.5f, Color? color = null,
        float labelWeight = 3.2f, float valueWeight = 1)
    {
        string font = bold ? StandardFonts.CourierBold : StandardFonts.Courier;
        HorizontalStack line = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 6, Margin = new Thickness(0, 1.5f, 0, 1.5f) };
        line.Add(new TextBlock(label) { FontName = font, FontSize = size, Color = color ?? Ink, Alignment = TextAlignment.Left }, labelWeight);
        line.Add(new TextBlock(value) { FontName = font, FontSize = size, Color = color ?? Ink, Alignment = TextAlignment.Right }, valueWeight);
        return line;
    }

    /// <summary>A dashed tear-line rule, the way a thermal printer separates sections.</summary>
    private static Line Rule() => new()
    {
        Thickness = 0.8f,
        Color = Mute,
        DashPattern = [3, 4],
        Margin = new Thickness(0, 10, 0, 10),
    };

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string dir = FindAssetsDir();
        return RequiredAssets.ToDictionary(key => key, key => File.ReadAllBytes(Path.Combine(dir, key)), StringComparer.Ordinal);
    }

    private static byte[]? GetOptionalAsset(IReadOnlyDictionary<string, byte[]>? assets, string key)
    {
        if (assets is not null && assets.TryGetValue(key, out byte[]? bytes)) return bytes;
        string? dir = TryFindAssetsDir();
        if (dir is not null)
        {
            string candidate = Path.Combine(dir, key);
            if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
        }
        return null;
    }

    private static string FindAssetsDir() =>
        TryFindAssetsDir() ?? throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");

    private static string? TryFindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate)) return candidate;
            string local = Path.Combine(dir, "assets");
            if (Directory.Exists(local)) return local;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
