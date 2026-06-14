using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// PDF/UA-1 tagged custom-fonts example.
///
/// Embeds the bundled Liberation Sans TTF as the document default font so every element
/// inherits it. Section headings are /Hx structure elements; body paragraphs are /P.
/// PDF/UA-1 requires all fonts to be embedded - the TTF embedding satisfies that rule.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Custom Fonts (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged document with an embedded TrueType font - each section uses a semantic heading.")]
public static class CustomFontTaggedExample
{
    // NotoSans-covered symbols rendered with the default font; arrows and the
    // dingbat check mark are routed to NotoSansMath / NotoSansSymbols2 below.
    private static readonly string[] UnicodeSymbols =
    {
        "• Bullet point (U+2022)",
        "© Copyright symbol (U+00A9)",
        "® Registered trademark (U+00AE)",
        "™ Trademark (U+2122)",
        "– En-dash (U+2013) - Em-dash (U+2014)",
    };

    /// <summary>
    /// Generates the tagged custom-font demo. Returns <c>null</c> if no suitable TTF is found.
    /// </summary>
    public static Document? Generate()
    {
        // Embed the bundled NotoSans TTF as the document default (see BundledFont).
        string fontPath = BundledFont.SansPath;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Embedded Font Demo - Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates TrueType font embedding in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, font, truetype, unicode",
            },
            Settings = new DocumentSettings
            {
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(fontPath)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        doc.Add(new Heading("TrueType Font Embedding Demo (PDF/UA-1)")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "This document embeds a TrueType font (Noto Sans) as the document-wide default. " +
                     "PDF/UA-1 requires all fonts to be embedded so that text reflow and copy/paste work " +
                     "correctly for assistive technology.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── Unicode symbols ────────────────────────────────────────────────────

        doc.Add(new Heading("Unicode Symbols")
        {
            TocIdentifier = "unicode",
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 0, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text   = "Embedded TrueType fonts support the full Unicode range. Each symbol below is tagged as part of a /P paragraph:",
            Margin = new Thickness(0, 0, 0, 4),
        });

        foreach (string sym in UnicodeSymbols)
        {
            doc.Add(new TextBlock { Text = sym, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
        }

        // Arrows live in NotoSansMath; the check mark (a dingbat) in NotoSansSymbols2.
        doc.Add(new TextBlock { Text = "← ↑ → ↓ Directional arrows", FontName = BundledFont.MathPath, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
        doc.Add(new TextBlock { Text = "✓ Check mark (U+2713)", FontName = BundledFont.SymbolsPath, FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });

        // ── Currency symbols ───────────────────────────────────────────────────

        doc.Add(new Heading("Currency Symbols")
        {
            TocIdentifier = "currency",
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 10, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text   = "€ Euro  £ Pound  ¥ Yen  ₹ Rupee",
            FontSize = 12,
        });

        // ── Mathematical symbols ───────────────────────────────────────────────

        doc.Add(new Heading("Mathematical Symbols")
        {
            TocIdentifier = "math",
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 10, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text     = "± Plus-minus  × Multiply  ÷ Divide  ≠ Not equal  ∞ Infinity",
            FontName = BundledFont.MathPath,
            FontSize = 12,
        });

        // ── Greek letters ──────────────────────────────────────────────────────

        doc.Add(new Heading("Greek Letters")
        {
            TocIdentifier = "greek",
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 10, 0, 6),
        });

        doc.Add(new TextBlock
        {
            Text     = "α Alpha  β Beta  γ Gamma  δ Delta  π Pi  Ω Omega",
            FontSize = 12,
            Margin   = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text   = $"© {DateTime.Now.Year} BonniePDF • TrueType Font Embedding",
            FontSize = 9,
            TextColor = ColorPalette.Slate[400],
        });

        return doc;
    }
}
