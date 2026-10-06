using System.Linq;
using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.Elements.FlagsOfTheWorld;

/// <summary>
/// SVG import - twelve national flags drawn as vector content, from a handful of
/// shapes to a coat of arms of hundreds of paths and gradients. PDF/A-2a + PDF/UA-1:
/// every flag carries alt text. Nothing here is a bitmap, so the flag on the second
/// page stays sharp at any zoom.
///
/// The flags are SVG files supplied as assets (see <see cref="RequiredAssets"/>);
/// <c>Svg</c> takes the markup as a string.
/// </summary>
[System.ComponentModel.DisplayName("Flags of the World")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("SVG import: twelve national flags drawn as vector content, from a few shapes to a coat of arms with eight gradients, and one flag at full page width that stays sharp at any zoom. Every flag carries alt text.")]
public static class FlagsOfTheWorldExample
{
    /// <summary>A flag's <c>flag-icons</c> country code and its display name. Mexico first: it is the full-width flag.</summary>
    private readonly record struct Flag(string Code, string Name);

    private static readonly Flag[] Flags =
    {
        new("mx", "Mexico"),
        new("gs", "South Georgia"),
        new("es", "Spain"),
        new("pt", "Portugal"),
        new("gr", "Greece"),
        new("nl", "Netherlands"),
        new("hk", "Hong Kong"),
        new("kz", "Kazakhstan"),
        new("co", "Colombia"),
        new("is", "Iceland"),
        new("lb", "Lebanon"),
        new("my", "Malaysia"),
    };

    private const int Columns = 3;

    /// <summary>Width of a flag in the grid, in points: a third of the page less cell padding, card padding and border.</summary>
    private const float CardFlagWidth = 140;

    /// <summary>A4 (595 pt) less the default 40 pt margins.</summary>
    private const float PageContentWidth = 515;

    /// <summary>The asset paths (relative to <c>Examples/assets/</c>) the example needs pre-loaded.</summary>
    public static IReadOnlyList<string> RequiredAssets { get; } = Flags.Select(AssetPath).ToArray();

    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Flags of the World",
                Author   = "BonniePDF",
                Subject  = "SVG import - twelve national flags drawn as vector content, one shown at full page width.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, svg, vector, flags, figure, alt-text",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[700]),
            },
        };

        AddFlagGrid(doc, assets);
        AddFullWidthFlag(doc, assets);
        AddCredits(doc);

        return doc;
    }

    /// <summary>Page 1: the title, an introduction and the grid of flag cards.</summary>
    private static void AddFlagGrid(Document doc, IReadOnlyDictionary<string, byte[]> assets)
    {
        doc.Add(new Heading("Flags of the World")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 24,
            Color     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "Each flag below is an SVG file imported as vector content: paths, fills and gradients become native PDF " +
                     "drawing operations. The set runs from Iceland and Colombia, a few shapes each, to South Georgia, whose " +
                     "coat of arms carries eight gradients.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        Grid grid = new(1, 1, 1)
        {
            CellPadding = 6,
            Border      = Border.None,
        };
        for (int i = 0; i < Flags.Length; i += Columns)
            grid.AddRow(Flags.Skip(i).Take(Columns).Select(flag => FlagCard(flag, assets)).ToArray());
        doc.Add(grid);
    }

    /// <summary>Page 2: the most intricate flag, Mexico, at the full width of the page.</summary>
    private static void AddFullWidthFlag(Document doc, IReadOnlyDictionary<string, byte[]> assets)
    {
        doc.Add(new PageBreak());

        doc.Add(new Heading("Mexico at Full Width")
        {
            TocIdentifier = "s-full-width",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 15,
            Color     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 0, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text      = "The flag of Mexico is 356 paths and four gradients. Zoom in: the eagle, the serpent and every feather stay " +
                        "crisp at any scale.",
            Color = ColorPalette.Slate[500],
            Margin    = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new Svg
        {
            SvgData = FlagMarkup(Flags[0], assets),
            Width   = PageContentWidth,
            AltText = AltText(Flags[0]),
        });
    }

    /// <summary>The credits block naming the flag artwork's source, version and licence.</summary>
    private static void AddCredits(Document doc)
    {
        doc.Add(new Heading("Credits")
        {
            TocIdentifier = "s-credits",
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 15,
            Color     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 16, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text      = "Flag artwork: flag-icons 7.2.3 (MIT licence), https://github.com/lipis/flag-icons.",
            FontSize  = 9,
            Color = ColorPalette.Slate[500],
        });
    }

    private static string AssetPath(Flag flag) => $"flags/4x3/{flag.Code}.svg";

    private static string AltText(Flag flag) => $"Flag of {flag.Name}";

    /// <summary>Reads a flag's SVG markup from the asset map, failing loudly if it was not pre-loaded.</summary>
    private static string FlagMarkup(Flag flag, IReadOnlyDictionary<string, byte[]> assets)
    {
        if (!assets.TryGetValue(AssetPath(flag), out byte[]? bytes))
            throw new InvalidOperationException($"Missing required asset: {AssetPath(flag)}");
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>A bordered card holding a flag and its name.</summary>
    private static Container FlagCard(Flag flag, IReadOnlyDictionary<string, byte[]> assets)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new Svg
        {
            SvgData = FlagMarkup(flag, assets),
            Width   = CardFlagWidth,
            AltText = AltText(flag),
        });
        stack.Add(new TextBlock
        {
            Text      = flag.Name,
            FontSize  = 9,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
        });

        return new Container
        {
            Padding      = new Thickness(8),
            Border       = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            CornerRadius = 4,
            Child        = stack,
        };
    }
}
