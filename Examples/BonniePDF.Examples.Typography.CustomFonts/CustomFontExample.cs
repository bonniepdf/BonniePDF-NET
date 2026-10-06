using BonniePDF;

namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// TrueType font embedding with Unicode coverage.
/// Symbols, currency, arrows, maths, dingbats, and Greek across three Noto fonts.
/// PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Custom Fonts")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("TrueType font embedding with Unicode coverage — symbols, currency, arrows, maths, dingbats, and Greek. PDF/A-2a + PDF/UA-1.")]
public static class CustomFontExample
{
    public static Document Generate()
    {
        string font = BundledFont.SansPath;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Custom Fonts",
                Author   = "BonniePDF",
                Subject  = "TrueType font embedding with Unicode coverage. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, fonts, unicode, truetype",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(font)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Custom Fonts — TrueType Embedding")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = font,
            FontSize      = 22,
            Color     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "BonniePDF embeds compact glyph subsets of TrueType fonts directly in the PDF — " +
                     "only the characters actually used are stored. Fonts with different Unicode coverage " +
                     "can be mixed within a single document by setting FontName per block.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── 1. Unicode Symbols ────────────────────────────────────────────────

        Section(doc, "Unicode Symbols", "s-symbols");

        foreach (string text in new[]
        {
            "• Bullet (U+2022)  © Copyright (U+00A9)  ® Registered (U+00AE)  ™ Trademark (U+2122)",
            "– En-dash (U+2013)  — Em-dash (U+2014)",
            "± Plus-minus  × Multiply  ÷ Divide",
        })
        {
            doc.Add(new TextBlock { Text = text, Margin = new Thickness(0, 0, 0, 4) });
        }

        // ── 2. Currency Symbols ───────────────────────────────────────────────

        Section(doc, "Currency Symbols", "s-currency");

        doc.Add(new TextBlock
        {
            Text   = "€ Euro  £ Pound  ¥ Yen  ₹ Rupee  ₿ Bitcoin",
            Margin = new Thickness(0, 0, 0, 4),
        });

        // ── 3. Arrows ─────────────────────────────────────────────────────────

        Section(doc, "Arrows (via NotoSansMath)", "s-arrows");

        doc.Add(new TextBlock
        {
            Text     = "← ↑ → ↓  ↔ ↕  ⇐ ⇒ ⇔  ↩ ↪",
            FontName = BundledFont.MathPath,
            Margin   = new Thickness(0, 0, 0, 4),
        });

        // ── 4. Mathematical Operators ─────────────────────────────────────────

        Section(doc, "Mathematical Operators (via NotoSansMath)", "s-math");

        foreach (string text in new[]
        {
            "≠ Not equal  ≤ Less-equal  ≥ Greater-equal  ∞ Infinity",
            "∑ Sum  ∏ Product  √ Root  ∫ Integral",
        })
        {
            doc.Add(new TextBlock { Text = text, FontName = BundledFont.MathPath, Margin = new Thickness(0, 0, 0, 4) });
        }

        // ── 5. Dingbats ───────────────────────────────────────────────────────

        Section(doc, "Dingbats (via NotoSansSymbols2)", "s-dingbats");

        doc.Add(new TextBlock
        {
            Text     = "✓ Check  ✗ Cross  ★ Star  ♠ Spade  ♥ Heart  ♦ Diamond  ♣ Club",
            FontName = BundledFont.SymbolsPath,
            Margin   = new Thickness(0, 0, 0, 4),
        });

        // ── 6. Greek Letters ──────────────────────────────────────────────────

        Section(doc, "Greek Letters", "s-greek");

        doc.Add(new TextBlock
        {
            Text   = "α Alpha  β Beta  γ Gamma  δ Delta  π Pi  Ω Omega",
            Margin = new Thickness(0, 0, 0, 4),
        });

        // ── 7. Standard vs Embedded ───────────────────────────────────────────

        Section(doc, "Standard vs Embedded", "s-comparison");

        doc.Add(new TextBlock
        {
            Text     = "Standard Helvetica (Type 1) — limited to the WinAnsi charset.",
            FontName = StandardFonts.Helvetica,
            Margin   = new Thickness(0, 0, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text = "Embedded Noto Sans (TrueType) — full Unicode range, subsetting keeps file size compact.",
        });

        return doc;
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontName      = BundledFont.SansPath,
            FontSize      = 13,
            Color     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 12, 0, 6),
        });
    }
}
