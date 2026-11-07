using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>An international customer list in Helvetica, with Polish, Greek, Cyrillic, Hebrew, Arabic and maths falling back to fonts that cover them.</summary>
[System.ComponentModel.DisplayName("Font Fallback & Missing Characters")]
[System.ComponentModel.Category("Fonts")]
[System.ComponentModel.Description("An international customer list in Helvetica: Polish, Greek, Cyrillic and Hebrew names fall back to Liberation Sans, Arabic and maths symbols to registered fonts, under strict missing-character checks.")]
public static class FontFallbackExample
{
    /// <summary>The fallback fonts this document registers, in the order they are tried.</summary>
    private static readonly (string File, string Family)[] Fonts =
    [
        ("NotoNaskhArabic-Regular.ttf", "Noto Naskh Arabic"),
        ("NotoSansMath-Regular.ttf", "Noto Sans Math"),
    ];

    /// <summary>Customer, city, order total, and the font that draws the name.</summary>
    private static readonly (string Customer, string City, string Total, string Font)[] Customers =
    [
        ("Zoë Müller", "Zürich", "CHF 1,240", "Helvetica"),
        ("Łukasz Wójcik", "Łódź", "4 980 zł", "Liberation Sans, Helvetica’s substitute"),
        ("Αλεξάνδρα Παππά", "Αθήνα", "€ 2,315", "Liberation Sans"),
        ("Дмитрий Соколов", "Санкт-Петербург", "€ 1,870", "Liberation Sans"),
        ("יעל כהן", "חיפה", "₪ 3,900", "Liberation Sans"),
        ("نور حداد", "عمّان", "JOD 640", "Noto Naskh Arabic, registered here"),
    ];

    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            // Strict checking: a character no font can draw is an error, not a silent gap.
            MissingGlyphs = MissingGlyphs.Throw,
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10.5f).WithColor(ColorPalette.Slate[700])
        })
        { Metadata = new DocumentMetadata { Title = "Font fallback and missing characters" } };
        foreach (var (file, family) in Fonts)
            doc.Fonts.Add(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", file)), family: family);

        doc.Add(new Heading("International customers")
        {
            Level = 1,
            FontFamily = "Helvetica",
            FontWeight = FontWeight.Bold,
            FontSize = 24,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 0, 0, 6)
        });
        doc.Add(new TextBlock("Everything here is set in Helvetica. Where a name needs a character Helvetica does not have, that run falls back: first to Liberation Sans, Helvetica’s metric-compatible substitute, then to the fonts this document registers, in order.")
        {
            LineHeight = 1.45f,
            Margin = new Thickness(0, 0, 0, 16)
        });

        Table table = new(2.4f, 2.2f, 1.3f, 2.6f) { CellPadding = 7, HeaderBackgroundColor = ColorPalette.Slate[800], HeaderTextColor = Color.White };
        table.AddHeader(Cell("Customer", bold: true), Cell("City", bold: true), Cell("Orders", bold: true, right: true), Cell("Name drawn in", bold: true));
        foreach (var (customer, city, total, font) in Customers)
            table.AddRow(Cell(customer, size: 12), Cell(city, size: 12), Cell(total, right: true), Cell(font, muted: true));
        doc.Add(table);

        doc.Add(new Heading("Symbols")
        {
            Level = 2,
            FontFamily = "Helvetica",
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            Color = ColorPalette.Slate[900],
            Margin = new Thickness(0, 20, 0, 6)
        });
        doc.Add(new TextBlock("Shipping rule:  ∀ parcel ∈ order:  weight ≤ 30 kg  ⇒  standard rate,  else  rate ⊕ surcharge") { FontSize = 12 });
        doc.Add(new TextBlock("∀, ∈, ⇒ and ⊕ come from Noto Sans Math, registered here; ≤ comes from Liberation Sans; the letters stay in Helvetica.")
        {
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 4, 0, 0)
        });

        doc.Add(new TextBlock("This document asks for strict glyph checking, so a character that no font covers raises MissingGlyphException instead of silently disappearing.")
        {
            Margin = new Thickness(0, 20, 0, 0),
            FontSize = 9.5f
        });
        return doc;
    }

    /// <summary>A deliberate failure suitable for CI; never put the unsupported character in a real invoice.</summary>
    public static async Task VerifyMissingCharacterAsync()
    {
        using var doc = Generate();
        doc.Add(new TextBlock(char.ConvertFromUtf32(0x10ffff)));
        try { await doc.GenerateBytesAsync(); }
        catch (MissingGlyphException) { return; }
        throw new InvalidOperationException("The unsupported character should fail strict glyph checking.");
    }

    /// <summary>A table cell: bold, right-aligned, muted or larger on request.</summary>
    private static TextBlock Cell(string text, bool bold = false, bool right = false, bool muted = false, float size = 10)
    {
        TextBlock block = new(text) { FontSize = size, Alignment = right ? TextAlignment.Right : TextAlignment.Left };
        if (bold) block.FontName = StandardFonts.HelveticaBold;
        if (muted) block.Color = ColorPalette.Slate[500];
        return block;
    }
}
