using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.CustomFonts;

/// <summary>
/// Demonstrates TrueType font embedding with full Unicode support using the Fluent API.
/// Sets the embedded font as the document-wide default so every text element
/// inherits it without an explicit .Font() call.
/// </summary>
[System.ComponentModel.DisplayName("Custom Fonts (Fluent API)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Embedding and using custom TTF fonts using the Fluent API.")]
public static class CustomFontsFluentExample
{

    private static readonly string[] UnicodeSymbols =
    {
        "\u2022 Bullet point (U+2022)",
        "\u00A9 Copyright symbol (U+00A9)",
        "\u00AE Registered trademark (U+00AE)",
        "\u2122 Trademark (U+2122)",
        "\u2013 En-dash (U+2013) \u2014 Em-dash (U+2014)",
        "\u2190 \u2191 \u2192 \u2193 Arrows",
        "\u2713 Check mark (U+2713)"
    };

    public static Document Generate()
    {
        // Prefer Arial; fall back to Segoe UI on systems where Arial is absent
        string arialPath = @"C:\Windows\Fonts\arial.ttf";
        if (!File.Exists(arialPath))
        {
            Console.WriteLine("Arial not found, trying Segoe UI...");
            arialPath = @"C:\Windows\Fonts\segoeui.ttf";
        }

        if (!File.Exists(arialPath))
        {
            Console.WriteLine("No suitable TTF font found. Skipping embedded font example.");
            return null;
        }

        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Embedded Font Example (Fluent)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Set the TTF path as the document default so all text inherits it
            doc.WithDefaultTextStyle(style => style
                .WithFamily(arialPath)
                .WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title — inherits Arial TTF, overrides size only
                    column.Item()
                        .Text("TrueType Font Embedding Demo (Fluent API)")
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 12);

                    column.Item()
                        .Text("Full Unicode Support with Embedded Fonts")
                        .FontSize(14)
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 16);

                    // ── Unicode Symbols ──────────────────────────────────────────
                    column.Item()
                        .Text("Unicode Symbols:")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    foreach (string sym in UnicodeSymbols)
                    {
                        column.Item()
                            .Text(sym)
                            .TextColor(Color.Slate600);
                    }

                    // Bottom margin on the last symbol line
                    column.Item().Margin(0, 0, 0, 12).Empty();

                    // ── Currency Symbols ─────────────────────────────────────────
                    column.Item()
                        .Text("Currency Symbols:")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    column.Item()
                        .Text("\u20AC Euro \u00A3 Pound \u00A5 Yen \u20B9 Rupee")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 12);

                    // ── Mathematical Symbols ─────────────────────────────────────
                    column.Item()
                        .Text("Mathematical Symbols:")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    column.Item()
                        .Text("\u00B1 Plus-minus \u00D7 Multiply \u00F7 Divide \u2260 Not equal")
                        .TextColor(Color.Slate600);

                    column.Item()
                        .Text("\u2264 Less-equal \u2265 Greater-equal \u221E Infinity")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 12);

                    // ── Greek Letters ────────────────────────────────────────────
                    column.Item()
                        .Text("Greek Letters:")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    column.Item()
                        .Text("\u03B1 Alpha \u03B2 Beta \u03B3 Gamma \u03B4 Delta \u03C0 Pi \u03A9 Omega")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 16);

                    // ── Mixed Fonts ──────────────────────────────────────────────
                    column.Item()
                        .Text("Mixed Font Demonstration:")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 6);

                    // Standard Helvetica (Type1 — not embedded)
                    column.Item()
                        .Text("This line uses standard Helvetica (Type1, not embedded)")
                        .Font(StandardFonts.Helvetica)
                        .TextColor(Color.Slate600);

                    // Embedded Arial (TrueType — inherits from default)
                    column.Item()
                        .Text("This line uses embedded Arial (TrueType, full Unicode)")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 16);

                    // Footer
                    column.Item()
                        .Text($"\u00A9 {DateTime.Now.Year} BonniePDF \u2022 TrueType Font Embedding")
                        .FontSize(10)
                        .TextColor(Color.Slate400);
                });
            });
        }).Build();

    }
}

