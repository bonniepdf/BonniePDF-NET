using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.Hospitality;

/// <summary>
/// A kitchen allergen guide on landscape A4: every dish against the 14 allergens named in UK food law,
/// marked contains or may contain, as a table screen readers can navigate.
/// </summary>
[System.ComponentModel.DisplayName("Allergen Guide")]
[System.ComponentModel.Category("Hospitality")]
[System.ComponentModel.Description("A kitchen allergen guide on landscape A4: every dish against the 14 allergens named in UK food law, marked contains or may contain, as a table screen readers can navigate.")]
public static class AllergenMatrixExample
{
    private const string SerifKey = "fonts/SourceSerif4-Regular.ttf";
    private const string SprigSvgKey = "images/svg/the-larder.svg";

    public static IReadOnlyList<string> RequiredAssets { get; } = [SerifKey, SprigSvgKey];

    /// <summary>The 14 allergens UK food law requires a business to declare, in the order the Food Standards Agency lists them.</summary>
    private static readonly string[] Allergens =
    [
        "Celery", "Cereals with gluten", "Crustaceans", "Eggs", "Fish", "Lupin", "Milk",
        "Molluscs", "Mustard", "Tree nuts", "Peanuts", "Sesame", "Soya", "Sulphites",
    ];

    /// <summary>The menu, by course: each dish with the allergens it contains and those it may contain traces of.</summary>
    private static readonly (string Course, (string Dish, string[] Contains, string[] Traces)[] Dishes)[] Courses =
    [
        ("Small plates",
        [
            ("Roast squash soup", ["Celery", "Cereals with gluten", "Milk"], ["Sesame"]),
            ("Cured trout", ["Cereals with gluten", "Fish", "Milk", "Mustard"], []),
            ("Crispy cauliflower", ["Milk"], ["Sesame"]),
        ]),
        ("Mains",
        [
            ("Braised beef cheek", ["Celery", "Milk", "Sulphites"], []),
            ("Hake, mussels & cider", ["Celery", "Fish", "Milk", "Molluscs", "Sulphites"], ["Crustaceans"]),
            ("Wild mushroom risotto", ["Celery", "Milk", "Sulphites"], []),
            ("Spiced chickpea stew", ["Celery", "Cereals with gluten", "Sesame"], ["Mustard"]),
        ]),
        ("Puddings",
        [
            ("Sticky toffee pudding", ["Cereals with gluten", "Eggs", "Milk", "Sulphites"], ["Tree nuts"]),
            ("Dark chocolate tart", ["Cereals with gluten", "Eggs", "Milk", "Soya"], ["Tree nuts", "Peanuts"]),
            ("Poached pear", [], ["Tree nuts"]),
        ]),
    ];

    /// <summary>How each answer is shown: its word, the chip's fill and the word's colour.</summary>
    private static readonly (string Word, Color? Fill, Color Ink) Contains = ("Yes", ColorPalette.Rose[100], ColorPalette.Rose[800]);
    private static readonly (string Word, Color? Fill, Color Ink) Trace = ("Trace", ColorPalette.Amber[100], ColorPalette.Amber[900]);
    private static readonly (string Word, Color? Fill, Color Ink) Absent = ("No", null, ColorPalette.Stone[500]);

    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Orientation = PageOrientation.Landscape,
                Margin = 30,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(ColorPalette.Stone[700]),
            },
            Metadata = new DocumentMetadata
            {
                Title = "The Larder: allergen guide",
                Author = "The Larder",
                Subject = "Allergens in every dish on the dinner menu",
            },
        };
        string serif = doc.Fonts.Add(GetAsset(assets, SerifKey)).FontName;
        string sprigSvg = Encoding.UTF8.GetString(GetAsset(assets, SprigSvgKey));

        // ── Title and review line ──
        HorizontalStack titleRow = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 8 };
        titleRow.Add(new Svg { SvgData = sprigSvg, Width = 24, Height = 24, IsDecorative = true, Margin = new Thickness(0, 2, 0, 0) });
        titleRow.Add(new Heading("Allergen guide") { Level = 1, FontName = serif, FontSize = 24, Color = ColorPalette.Stone[900] });

        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Equal, Margin = new Thickness(0, 0, 0, 12) };
        top.Add(titleRow);
        top.Add(new TextBlock("The Larder  ·  Dinner menu\nReviewed 5 November 2026 by the head chef")
        {
            Alignment = TextAlignment.Right,
            Color = ColorPalette.Stone[500],
        });
        doc.Add(top);

        // ── The matrix: one row per dish, one column per allergen ──
        Table matrix = new([3.2f, .. Allergens.Select(_ => 1f)])
        {
            CellPadding = 7,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Stone[300]),
            HeaderBackgroundColor = ColorPalette.Stone[900],
            HeaderTextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 12),
        };
        TableCell[] header =
        [
            new TableCell { Content = new TextBlock("Dish") { FontName = StandardFonts.HelveticaBold, FontSize = 8 } },
            .. Allergens.Select(name => new TableCell
            {
                Padding = new Thickness(2, 7, 2, 7),
                Content = new TextBlock(name) { FontName = StandardFonts.HelveticaBold, FontSize = 7, Alignment = TextAlignment.Center },
            }),
        ];
        matrix.AddHeader(header);
        foreach (var (course, dishes) in Courses)
        {
            matrix.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell
                    {
                        ColumnSpan = Allergens.Length + 1,
                        BackgroundColor = ColorPalette.Amber[50],
                        Content = new TextBlock(course) { FontName = serif, FontSize = 11, Color = ColorPalette.Amber[800] },
                    },
                ],
            });
            foreach (var (dish, contains, traces) in dishes)
            {
                matrix.AddRow(new TableRow
                {
                    Cells =
                    [
                        new TableCell { Content = new TextBlock(dish) { FontName = serif, FontSize = 11, Color = ColorPalette.Stone[900] } },
                        .. Allergens.Select(name => Answer(contains.Contains(name) ? Contains : traces.Contains(name) ? Trace : Absent)),
                    ],
                });
            }
        }
        doc.Add(matrix);

        // ── Key and kitchen note ──
        HorizontalStack key = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 6, Margin = new Thickness(0, 0, 0, 8) };
        key.Add(Swatch(Contains));
        key.Add(new TextBlock("The dish contains this allergen as an ingredient.") { FontSize = 8, Margin = new Thickness(0, 0, 14, 0) });
        key.Add(Swatch(Trace));
        key.Add(new TextBlock("Made in a kitchen that handles it; traces are possible.") { FontSize = 8 });
        doc.Add(key);
        doc.Add(new TextBlock(
            "Recipes and suppliers change. Please tell your server about any allergy before you order, and ask to see the recipe sheet for a dish; we cannot guarantee that any dish is free of an allergen.")
        {
            FontSize = 8,
            Color = ColorPalette.Stone[500],
        });
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>One cell of the matrix: the answer as a word, on a tinted chip that shows it at a glance.</summary>
    private static TableCell Answer((string Word, Color? Fill, Color Ink) answer) => new()
    {
        Padding = new Thickness(3),
        Content = new Container
        {
            BackgroundColor = answer.Fill,
            CornerRadius = 3,
            Padding = new Thickness(4),
            Child = new TextBlock(answer.Word)
            {
                FontName = answer.Word == "No" ? StandardFonts.Helvetica : StandardFonts.HelveticaBold,
                FontSize = 7.5f,
                Color = answer.Ink,
                Alignment = TextAlignment.Center,
            },
        },
    };

    /// <summary>The key's sample of an answer cell.</summary>
    private static Container Swatch((string Word, Color? Fill, Color Ink) answer) => new()
    {
        BackgroundColor = answer.Fill,
        CornerRadius = 3,
        Padding = new Thickness(6, 2, 6, 2),
        Child = new TextBlock(answer.Word) { FontName = StandardFonts.HelveticaBold, FontSize = 7.5f, Color = answer.Ink },
    };

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string dir = FindAssetsDir();
        return RequiredAssets.ToDictionary(key => key, key => File.ReadAllBytes(Path.Combine(dir, key)), StringComparer.Ordinal);
    }

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string key)
    {
        if (assets.TryGetValue(key, out byte[]? bytes)) return bytes;
        string candidate = Path.Combine(FindAssetsDir(), key);
        if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
        throw new InvalidOperationException($"Supply {key}.");
    }

    private static string FindAssetsDir()
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
        throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");
    }
}
