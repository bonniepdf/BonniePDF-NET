using BonniePDF;

namespace BonniePDF.Examples.Typography.InternationalTypography;

/// <summary>
/// International typography: OpenType kerning and ligatures from an embedded serif, a kerning-off comparison,
/// Arabic and Hebrew set right to left, a mirrored table, and opt-in colour emoji.
///
/// Embedded fonts are shaped with their own OpenType tables, so pair kerning and ligatures apply by default;
/// FontFeatures switches them off per element. Right-to-left text is a direction, not a different kind of
/// element: Settings.ContentDirection sets a document default and any element can override it with
/// ContentDirection. A HorizontalStack or Table set to RightToLeft mirrors its columns while keeping the
/// children in source order, and TextAlignment.Start follows the direction.
///
/// Limits today: plain text blocks, rows and tables are direction-aware. Styled spans, lists and page-number
/// tokens are not yet fully integrated with right-to-left text, so this example uses none of them.
/// </summary>
[System.ComponentModel.DisplayName("International Typography")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("OpenType kerning and ligatures compared on and off, Arabic and Hebrew set right to left, one table in two directions, and opt-in colour flag emoji. PDF/A-2b and PDF/UA-1.")]
public static class InternationalTypographyExample
{
    /// <summary>Builds the document. Supply the font assets or use the bundled example fonts.</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "International Typography",
                Author = "BonniePDF",
                Subject = "Kerning, ligatures, Arabic and Hebrew right-to-left text, a mirrored table and colour emoji.",
                Keywords = "pdf, typography, kerning, ligatures, arabic, hebrew, rtl, bidi, emoji",
            },
            Settings = new DocumentSettings(PdfSize.A4, 36)
            {
                EmojiRendering = EmojiRendering.Colour,
                Conformance = PdfAConformance.PdfA2b,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        string Font(string name) => doc.Fonts.Add(assets is not null && assets.TryGetValue("fonts/" + name, out var bytes)
            ? bytes : File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", name))).FontName;
        string serif = Font("SourceSerif4-Regular.ttf"),
            arabic = Font("NotoNaskhArabic-Regular.ttf"),
            hebrew = Font("NotoSerifHebrew-Regular.ttf"),
            emoji = Font("TwemojiMozilla.ttf");
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily(serif).WithSize(11).WithColor(ColorPalette.Slate[700]);

        // ── Title ─────────────────────────────────────────────────────────────

        doc.Add(new Heading("Typeset for every script")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = serif,
            FontSize = 28,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 4),
        });
        doc.Add(new TextBlock("Fonts are shaped with their own OpenType tables, so kerning, ligatures and joined scripts come out as the type designer drew them, and the line breaks you measure are the ones in the PDF.")
        {
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 14),
        });

        // ── Kerning and ligatures ─────────────────────────────────────────────

        doc.Add(SectionHeading("Kerning and ligatures", "kerning", serif));
        doc.Add(Specimen("Off · font features kern and liga set to 0", serif, ColorPalette.Slate[100],
            new Dictionary<string, int> { ["kern"] = 0, ["liga"] = 0 }));
        doc.Add(Specimen("On · the default for embedded fonts", serif, ColorPalette.Indigo[50]));

        // ── Right to left ─────────────────────────────────────────────────────

        doc.Add(SectionHeading("Right to left, mirrored", "rtl", serif));
        doc.Add(new TextBlock("This row has a right-to-left content direction. Its two children stay in source order, Arabic first, and the row mirrors them. Arabic letters join automatically, and the digits of 2026 still read left to right.")
        {
            Margin = new Thickness(0, 0, 0, 8),
        });
        var rtlRow = new HorizontalStack
        {
            ContentDirection = ContentDirection.RightToLeft,
            Spacing = 16,
            WidthDistribution = WidthDistribution.Equal,
            Margin = new Thickness(0, 0, 0, 14),
        };
        rtlRow.Add(new TextBlock("ينسّق النص العربي والعبري من اليمين إلى اليسار، ويصل الحروف ببعضها تلقائياً، ويحافظ على اتجاه الأرقام مثل 2026 داخل الفقرة نفسها.")
        {
            FontName = arabic,
            FontSize = 15,
            LineHeight = 1.6f,
            Alignment = TextAlignment.Start,
            Color = ColorPalette.Slate[800],
        });
        rtlRow.Add(new TextBlock("הטקסט העברי והערבי נערך מימין לשמאל והאותיות הערביות מתחברות אוטומטית בתוך אותה פסקה")
        {
            FontName = hebrew,
            FontSize = 15,
            LineHeight = 1.5f,
            Alignment = TextAlignment.Start,
            Color = ColorPalette.Slate[800],
        });
        doc.Add(rtlRow);

        // ── Mirrored table ────────────────────────────────────────────────────

        doc.Add(SectionHeading("One table, two directions", "table", serif));
        var tables = new HorizontalStack
        {
            Spacing = 16,
            WidthDistribution = WidthDistribution.Equal,
            Margin = new Thickness(0, 0, 0, 14),
        };
        tables.Add(Menu(ContentDirection.LeftToRight, ["Item", "Qty", "Price"], ["Coffee", "Tea", "Cake"], serif));
        tables.Add(Menu(ContentDirection.RightToLeft, ["الصنف", "الكمية", "السعر"], ["قهوة", "شاي", "كعكة"], arabic));
        doc.Add(tables);

        // ── Colour emoji ──────────────────────────────────────────────────────

        doc.Add(SectionHeading("Colour emoji, when you ask for it", "emoji", serif));
        doc.Add(new TextBlock("🇬🇧 🇸🇦 🇮🇱 🇩🇪 🇯🇵") { FontName = emoji, FontSize = 24, Margin = new Thickness(0, 0, 0, 10) });
        doc.Add(new TextBlock("Set in Source Serif 4, Noto Naskh Arabic and Noto Serif Hebrew under the SIL Open Font License; flag artwork from Twemoji (CC BY 4.0), font by Mozilla.")
        {
            FontSize = 8,
            Color = ColorPalette.Slate[500],
        });

        return doc;
    }

    // Local helpers and sample data used by this document.

    private static readonly (string Qty, string Price)[] Rows = [("2", "4.50"), ("1", "3.20"), ("3", "5.00")];

    private static Heading SectionHeading(string title, string tocId, string fontName) => new(title)
    {
        TocIdentifier = tocId,
        Level = 2,
        FontName = fontName,
        FontSize = 15,
        Color = ColorPalette.Indigo[600],
        Margin = new Thickness(0, 0, 0, 8),
    };

    // One kerning/ligature panel: the same two lines, with or without font features.
    private static Container Specimen(string label, string fontName, Color background, IReadOnlyDictionary<string, int>? fontFeatures = null)
    {
        var stack = new VerticalStack();
        stack.Add(new TextBlock(label) { FontName = fontName, FontSize = 8.5f, Color = ColorPalette.Slate[600], Margin = new Thickness(0, 0, 0, 4) });
        stack.Add(new TextBlock("AVATAR To Wave") { FontName = fontName, FontSize = 40, Color = ColorPalette.Slate[800], FontFeatures = fontFeatures });
        stack.Add(new TextBlock("office affluent flagship") { FontName = fontName, FontSize = 27, Color = ColorPalette.Slate[800], FontFeatures = fontFeatures });
        return new Container
        {
            Padding = new Thickness(10),
            BackgroundColor = background,
            CornerRadius = 6,
            Margin = new Thickness(0, 0, 0, 10),
            Child = stack,
        };
    }

    // The same data set as a table, in one direction. Source order never changes.
    private static Table Menu(ContentDirection contentDirection, string[] header, string[] items, string fontName)
    {
        TextBlock Cell(string text, Color color) => new(text) { FontName = fontName, FontSize = 12, Color = color, Alignment = TextAlignment.Start };
        var table = new Table(3f, 1f, 1f)
        {
            ContentDirection = contentDirection,
            CellPadding = 5,
            HeaderBackgroundColor = ColorPalette.Slate[800],
            HeaderTextColor = Color.White,
        };
        table.AddHeader(header.Select(text => (IElement)Cell(text, Color.White)).ToArray());
        for (int i = 0; i < items.Length; i++)
            table.AddRow(Cell(items[i], ColorPalette.Slate[800]), Cell(Rows[i].Qty, ColorPalette.Slate[600]), Cell(Rows[i].Price, ColorPalette.Slate[600]));
        return table;
    }
}
