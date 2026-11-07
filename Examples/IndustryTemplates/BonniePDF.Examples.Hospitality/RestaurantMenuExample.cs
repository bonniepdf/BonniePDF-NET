using BonniePDF;

namespace BonniePDF.Examples.Hospitality;

/// <summary>
/// A bistro dinner menu on a tall custom page: a serif masthead, courses with dotted leaders to
/// right-aligned prices, and dietary marks with a key.
/// </summary>
[System.ComponentModel.DisplayName("Restaurant Menu")]
[System.ComponentModel.Category("Hospitality")]
[System.ComponentModel.Description("A bistro dinner menu on a tall custom page: a serif masthead, courses with dotted leaders to right-aligned prices, and dietary marks with a key.")]
public static class RestaurantMenuExample
{
    private const string SerifKey = "fonts/SourceSerif4-Regular.ttf";

    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[600];
    private static readonly Color Accent = ColorPalette.Amber[800];

    /// <summary>A tall menu card, about two thirds of A4 wide.</summary>
    private static readonly PdfSize Menu = new(397, 700, "Menu card");

    /// <summary>Dietary marks: each mark's letters, its meaning, and the colour of its badge.</summary>
    private static readonly (string Mark, string Meaning, Color Fill, Color Ink)[] Marks =
    [
        ("V", "Vegetarian", ColorPalette.Emerald[100], ColorPalette.Emerald[800]),
        ("VG", "Vegan", ColorPalette.Lime[100], ColorPalette.Lime[800]),
        ("GF", "Gluten free", ColorPalette.Amber[100], ColorPalette.Amber[900]),
    ];

    /// <summary>The menu, by course: dish, description, price in pounds, and its dietary marks.</summary>
    private static readonly (string Course, (string Dish, string Description, string Price, string[] Marks)[] Dishes)[] Courses =
    [
        ("Small plates",
        [
            ("Roast squash soup", "Sage brown butter, toasted seeds, sourdough", "7.50", ["V"]),
            ("Cured trout", "Pickled cucumber, dill crème fraîche, rye crisp", "9.50", ["GF"]),
            ("Crispy cauliflower", "Harissa, preserved lemon, herb yoghurt", "8.00", ["V", "GF"]),
        ]),
        ("Mains",
        [
            ("Braised beef cheek", "Celeriac purée, glazed shallots, red wine jus", "24.00", ["GF"]),
            ("Hake, mussels & cider", "Leeks, samphire, new potatoes", "22.50", []),
            ("Wild mushroom risotto", "Aged parmesan, truffle oil, rocket", "18.00", ["V", "GF"]),
            ("Spiced chickpea stew", "Charred flatbread, tahini, pomegranate", "16.50", ["VG"]),
        ]),
        ("Puddings",
        [
            ("Sticky toffee pudding", "Salted caramel, clotted cream", "8.00", ["V"]),
            ("Dark chocolate tart", "Blood orange, crème fraîche", "8.50", ["V"]),
            ("Poached pear", "Spiced syrup, oat crumble, coconut sorbet", "7.50", ["VG", "GF"]),
        ]),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = Menu,
                Margin = 34,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Body),
            },
            Metadata = new DocumentMetadata
            {
                Title = "The Larder: dinner menu",
                Author = "The Larder",
                Subject = "Dinner menu with dietary marks",
            },
        };
        if (!assets.TryGetValue(SerifKey, out byte[]? bytes))
            throw new InvalidOperationException($"Supply {SerifKey}.");
        string serif = doc.Fonts.Add(bytes).FontName;

        // ── Masthead ──
        VerticalStack masthead = new() { Spacing = 4 };
        masthead.Add(new Heading("The Larder")
        {
            Level = 1,
            FontName = serif,
            FontSize = 30,
            Color = Color.White,
            Alignment = TextAlignment.Center,
        });
        masthead.Add(new TextBlock("DINNER  ·  FROM 6PM")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 7.5f,
            LetterSpacing = 2,
            Color = ColorPalette.Amber[300],
            Alignment = TextAlignment.Center,
        });
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Stone[900],
            Padding = new Thickness(20, 18, 20, 18),
            Margin = new Thickness(0, 0, 0, 18),
            Child = masthead,
        });

        // ── Courses ──
        foreach (var (course, dishes) in Courses)
        {
            doc.Add(new Heading(course)
            {
                Level = 2,
                FontName = serif,
                FontSize = 17,
                Color = Accent,
                Alignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 8),
            });
            foreach (var (dish, description, price, marks) in dishes)
                doc.Add(MenuItem(serif, dish, description, price, marks));
        }

        // ── Dietary key ──
        HorizontalStack key = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 12, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var (mark, meaning, fill, ink) in Marks)
        {
            HorizontalStack entry = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 4 };
            entry.Add(Badge(mark, fill, ink));
            entry.Add(new TextBlock(meaning) { FontSize = 7.5f });
            key.Add(entry);
        }
        doc.Add(new Container
        {
            Border = new Border(0, 0.75f, 0, 0),
            BorderColors = new BorderColors(ColorPalette.Stone[300]),
            Padding = new Thickness(0, 4, 0, 0),
            Child = key,
        });
        doc.Add(new TextBlock("Please tell us about any allergies before you order; our allergen guide lists every dish.")
        {
            FontSize = 7.5f,
            Color = ColorPalette.Stone[500],
            Margin = new Thickness(0, 6, 0, 0),
        });
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>One dish: its name, dotted leaders and price on the first line; its description and dietary marks beneath.</summary>
    private static VerticalStack MenuItem(string serif, string dish, string description, string price, string[] marks)
    {
        HorizontalStack line = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 4 };
        // The name and price keep their own widths; the leaders take the rest.
        line.Add(new TextBlock(dish) { FontName = serif, FontSize = 12, Color = Ink });
        line.Add(Leaders(), 1);
        line.Add(new TextBlock(price) { FontName = serif, FontSize = 12, Color = Ink });
        HorizontalStack detail = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 5 };
        detail.Add(new TextBlock(description) { FontSize = 8.5f });
        foreach (string mark in marks)
        {
            var (_, _, fill, ink) = Array.Find(Marks, m => m.Mark == mark);
            detail.Add(Badge(mark, fill, ink));
        }
        VerticalStack item = new() { Spacing = 2, Margin = new Thickness(0, 0, 0, 10) };
        item.Add(line);
        item.Add(detail);
        return item;
    }

    /// <summary>A row of small dots from the dish to its price, drawn as decoration so screen readers skip it.</summary>
    private static CanvasElement Leaders() => new((canvas, width, height) =>
    {
        for (float x = 2; x < width - 2; x += 4) canvas.FillCircle(x, height - 3, 0.6f, ColorPalette.Stone[400]);
    })
    {
        DrawWidth = 0,
        DrawHeight = 13,
        IsDecorative = true,
    };

    /// <summary>A dietary mark as a small rounded badge.</summary>
    private static Container Badge(string mark, Color fill, Color ink) => new()
    {
        BackgroundColor = fill,
        CornerRadius = 3,
        Padding = new Thickness(3, 1, 3, 1),
        Child = new TextBlock(mark) { FontName = StandardFonts.HelveticaBold, FontSize = 6.5f, Color = ink },
    };
}
