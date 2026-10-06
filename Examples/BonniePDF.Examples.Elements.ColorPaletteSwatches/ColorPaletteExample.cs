using BonniePDF;


namespace BonniePDF.Examples.Elements.ColorPaletteSwatches;

/// <summary>
/// Draws the entire <see cref="BonniePDF.ColorPalette"/> as a swatch grid - all 22 Tailwind v4
/// ramps × 11 shades, rendered as little squares via <c>ColorPalette[name][shade]</c>. Doubles as a
/// living reference for the palette tokens.
/// </summary>
[System.ComponentModel.DisplayName("Colour ColorPalette")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("The full ColorPalette as a swatch grid of little squares.")]
public static class ColorPaletteExample
{
    // Layout constants for the swatch grid. Boxes are short so all 22 palettes
    // (× 11 shades) fit on a single A4 sheet; their width is filled to the page.
    private const float BoxHeight = 28f;
    private const float HGap = 4f;
    private const float VGap = 3f;
    private const float LabelWidth = 56f;

    // The 22 ramps paired with their lower-case names, in the same order as the TypeScript port.
    private static readonly (string Name, ColorPalette Ramp)[] Palettes =
    [
        ("slate", ColorPalette.Slate),
        ("gray", ColorPalette.Gray),
        ("zinc", ColorPalette.Zinc),
        ("neutral", ColorPalette.Neutral),
        ("stone", ColorPalette.Stone),
        ("red", ColorPalette.Red),
        ("orange", ColorPalette.Orange),
        ("amber", ColorPalette.Amber),
        ("yellow", ColorPalette.Yellow),
        ("lime", ColorPalette.Lime),
        ("green", ColorPalette.Green),
        ("emerald", ColorPalette.Emerald),
        ("teal", ColorPalette.Teal),
        ("cyan", ColorPalette.Cyan),
        ("sky", ColorPalette.Sky),
        ("blue", ColorPalette.Blue),
        ("indigo", ColorPalette.Indigo),
        ("violet", ColorPalette.Violet),
        ("purple", ColorPalette.Purple),
        ("fuchsia", ColorPalette.Fuchsia),
        ("pink", ColorPalette.Pink),
        ("rose", ColorPalette.Rose),
    ];

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 36,
            Language = "en-GB"
        });

        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "ColorPalette Swatches";
        doc.Metadata.Author = "BonniePDF";

        // --- Title ---
        doc.Add(new TextBlock
        {
            Text = "ColorPalette",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            Color = ColorPalette.Slate[800]
        });

        doc.Add(new TextBlock
        {
            Text = "Every Tailwind v4 ramp in the palette - 22 colours × 11 shades, drawn as ColorPalette[name][shade].",
            FontName = StandardFonts.Helvetica,
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 4, 0, 8)
        });

        // --- Grid ---
        VerticalStack grid = new() { Spacing = VGap };

        // Header row: an empty gutter above the labels, then a shade number per column.
        // Weighted distribution: the gutter keeps its fixed width (null weight), and the 11
        // columns share the remaining page width equally (weight 1 each).
        HorizontalStack header = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = HGap
        };
        header.Add(new Container { Width = LabelWidth });
        foreach (int shade in ColorPalette.Shades)
        {
            header.Add(HeaderCell(shade.ToString(System.Globalization.CultureInfo.InvariantCulture)), 1f);
        }
        grid.Add(header);

        // One row per palette, lightest (50) on the left to darkest (950) on the right.
        foreach ((string name, ColorPalette ramp) in Palettes)
        {
            // Fixed-width label (null weight) + 11 swatches that fill the rest equally (weight 1).
            HorizontalStack row = new()
            {
                WidthDistribution = WidthDistribution.Weighted,
                Spacing = HGap
            };
            row.Add(RowLabel(name));
            foreach (int shade in ColorPalette.Shades)
            {
                row.Add(Swatch(ramp, shade), 1f);
            }
            grid.Add(row);
        }

        doc.Add(grid);

        return doc;
    }

    /// <summary>A single colour box for one palette/shade.</summary>
    private static Container Swatch(ColorPalette ramp, int shade) => new()
    {
        // No fixed Width: under the row's Weighted distribution the stack stretches each swatch
        // to fill the page, so the 11 boxes span the full width minus the label gutter.
        Height = BoxHeight,
        BackgroundColor = ramp[shade],
        // A hairline border keeps the lightest shades (50) visible against the white page.
        Border = new Border(0.5f),
        BorderColors = new BorderColors(ColorPalette.Slate[200]),
        CornerRadius = 2
    };

    /// <summary>The left-hand row label carrying the palette name.</summary>
    private static Container RowLabel(string name) => new()
    {
        Width = LabelWidth,
        Height = BoxHeight,
        // Nudge the label down so it sits roughly centred against the square.
        Padding = new Thickness(0, 9, HGap, 0),
        Child = new TextBlock
        {
            Text = name,
            FontName = StandardFonts.Helvetica,
            FontSize = 9,
            Color = ColorPalette.Slate[600]
        }
    };

    /// <summary>A centred header cell carrying a shade number (50, 100, … 950).</summary>
    // Centred via Alignment; no wrapping Container needed — the cell it lands in
    // already stretches with its column so the number stays centred over the swatch.
    private static TextBlock HeaderCell(string label) => new()
    {
        Text = label,
        FontName = StandardFonts.Helvetica,
        FontSize = 7,
        Color = ColorPalette.Slate[500],
        Alignment = TextAlignment.Center
    };
}
