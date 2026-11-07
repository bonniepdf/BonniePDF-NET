using BonniePDF;

namespace BonniePDF.Examples.Fonts.CustomFonts;

/// <summary>
/// TrueType font embedding with Unicode coverage.
/// Symbols, currency, arrows, maths, dingbats, and Greek across three Noto fonts.
/// PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Custom Fonts")]
[System.ComponentModel.Category("Fonts")]
[System.ComponentModel.Description(
    "TrueType font embedding: a large specimen of the embedded face, then Unicode coverage in two columns — symbols, currency, arrows, maths, dingbats, and Greek. PDF/A-2a + PDF/UA-1.")]
public static class CustomFontExample
{
    /// <summary>Which registered face a group of characters is set in.</summary>
    private enum FontRole { Text, Math, Symbols }

    /// <summary>The coverage groups, in two columns: title, contents id, face, and the lines that show them.</summary>
    private static readonly (string Title, string TocId, FontRole Role, string[] Lines)[] LeftGroups =
    [
        ("Unicode Symbols", "s-symbols", FontRole.Text, ["• Bullet  © Copyright  ® Registered  ™ Trademark", "– En-dash  — Em-dash  ± Plus-minus  × Multiply  ÷ Divide"]),
        ("Currency Symbols", "s-currency", FontRole.Text, ["€ Euro  £ Pound  ¥ Yen  ₹ Rupee  ₿ Bitcoin"]),
        ("Greek Letters", "s-greek", FontRole.Text, ["α Alpha  β Beta  γ Gamma  δ Delta  π Pi  Ω Omega"]),
    ];

    private static readonly (string Title, string TocId, FontRole Role, string[] Lines)[] RightGroups =
    [
        ("Arrows (via NotoSansMath)", "s-arrows", FontRole.Math, ["← ↑ → ↓  ↔ ↕  ⇐ ⇒ ⇔  ↩ ↪"]),
        ("Mathematical Operators (via NotoSansMath)", "s-math", FontRole.Math, ["≠ Not equal  ≤ Less-equal  ≥ Greater-equal  ∞ Infinity", "∑ Sum  ∏ Product  √ Root  ∫ Integral"]),
        ("Dingbats (via NotoSansSymbols2)", "s-dingbats", FontRole.Symbols, ["✓ Check  ✗ Cross  ★ Star  ♠ Spade  ♥ Heart  ♦ Diamond  ♣ Club"]),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        string font = Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSans-Regular.ttf");

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Custom Fonts",
                Author = "BonniePDF",
                Subject = "TrueType font embedding with Unicode coverage. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, fonts, unicode, truetype",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(font)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        byte[] ReadFont(string filename) => assets is not null && assets.TryGetValue("fonts/" + filename, out byte[]? bytes)
            ? bytes : File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", filename));
        font = doc.Fonts.Add(ReadFont("NotoSans-Regular.ttf")).FontName;
        string mathFont = doc.Fonts.Add(ReadFont("NotoSansMath-Regular.ttf")).FontName;
        string symbolsFont = doc.Fonts.Add(ReadFont("NotoSansSymbols2-Regular.ttf")).FontName;
        doc.Settings.DefaultTextStyle.WithFamily(font);

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Custom Fonts — TrueType Embedding")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = font,
            FontSize = 22,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text = "BonniePDF embeds compact glyph subsets of TrueType fonts directly in the PDF — " +
                   "only the characters actually used are stored. Fonts with different Unicode coverage " +
                   "can be mixed within a single document by setting FontName per block.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── The embedded face, large ───────────────────────────────────────────

        VerticalStack specimen = new() { Spacing = 4 };
        specimen.Add(new TextBlock("Aa") { FontName = font, FontSize = 64, Color = ColorPalette.Indigo[900] });
        specimen.Add(new TextBlock("Quick zephyrs blow, vexing daft Jim — £12, €15.") { FontName = font, FontSize = 20, Color = ColorPalette.Slate[900] });
        specimen.Add(new TextBlock("Ελληνικά  ·  Кириллица  ·  Tiếng Việt  ·  Ærøskøbing") { FontName = font, FontSize = 14, Color = ColorPalette.Slate[700] });
        specimen.Add(new TextBlock("Noto Sans Regular, embedded as a subset of only the glyphs on this page.")
        {
            FontName = font,
            FontSize = 9,
            Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 6, 0, 0),
        });
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Indigo[50],
            CornerRadius = 8,
            Padding = new Thickness(22, 16, 22, 16),
            Margin = new Thickness(0, 0, 0, 14),
            Child = specimen,
        });

        // ── Coverage, in two columns ───────────────────────────────────────────

        string FaceOf(FontRole role) => role switch { FontRole.Math => mathFont, FontRole.Symbols => symbolsFont, _ => font };
        HorizontalStack columns = new() { Spacing = 24, WidthDistribution = WidthDistribution.Equal };
        foreach (var groups in new[] { LeftGroups, RightGroups })
        {
            VerticalStack column = new();
            foreach (var (title, tocId, role, lines) in groups)
            {
                column.Add(SectionHeading(title, tocId, font));
                foreach (string text in lines)
                    column.Add(new TextBlock { Text = text, FontName = FaceOf(role), Margin = new Thickness(0, 0, 0, 4) });
            }
            columns.Add(column);
        }
        doc.Add(columns);

        // ── Standard vs Embedded ───────────────────────────────────────────────

        doc.Add(SectionHeading("Standard vs Embedded", "s-comparison", font));

        doc.Add(new TextBlock
        {
            Text = "Standard Helvetica (Type 1) — limited to the WinAnsi charset.",
            FontName = StandardFonts.Helvetica,
            Margin = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text = "Embedded NotoSans (TrueType) — full Unicode range, subsetting keeps file size compact.",
            FontName = font,
        });

        return doc;
    }

    private static Heading SectionHeading(string title, string tocId, string font) => new(title)
    {
        TocIdentifier = tocId,
        Level = 2,
        FontName = font,
        FontSize = 13,
        Color = ColorPalette.Indigo[600],
        Margin = new Thickness(0, 12, 0, 6),
    };
}
