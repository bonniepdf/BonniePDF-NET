using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

/// <summary>
/// Full Unicode range — BMP symbols, Greek, Cyrillic, dingbats, and supplementary emoji.
/// PDF/A is intentionally not claimed: emoji fonts include COLR/CBDT tables that archival
/// validators reject. PDF/UA-1 accessibility is still applied.
/// </summary>
[System.ComponentModel.DisplayName("Emojis & Unicode")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Full Unicode range — BMP symbols, Greek, Cyrillic, dingbats, and supplementary emoji across multiple embedded fonts. PDF/UA-1.")]
public static class EmojiExample
{
    public static Document Generate()
    {
        string textFont    = BundledFont.SansPath;
        string mathFont    = BundledFont.MathPath;
        string symbolsFont = BundledFont.SymbolsPath;
        string emojiFont   = BundledFont.EmojiPath;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Emojis & Unicode",
                Author   = "BonniePDF",
                Subject  = "Full Unicode range including supplementary planes. PDF/UA-1.",
                Keywords = "pdf, pdf/ua, accessibility, emoji, unicode, fonts",
            },
            Settings = new DocumentSettings
            {
                // PDF/A intentionally not claimed: emoji fonts include COLR/CBDT tables
                // that archival validators reject.
                Conformance   = PdfAConformance.None,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(textFont)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Emojis & Supplementary Unicode")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = textFont,
            FontSize      = 20,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "BonniePDF embeds TrueType subsets for the full Unicode range. " +
                     "Latin, Greek, and Cyrillic via NotoSans; arrows and maths via NotoSansMath; " +
                     "symbols and dingbats via NotoSansSymbols2; emoji via NotoEmoji.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        // ── BMP sections ───────────────────────────────────────────────────────

        BmpSection(doc, "Arrows (U+2190 – U+21FF)", "s-arrows",
            "← ↑ → ↓  ↔ ↕  ⇐ ⇒ ⇔  ↩ ↪  ↖ ↗ ↘ ↙", mathFont);

        BmpSection(doc, "Mathematical Operators (U+2200 – U+22FF)", "s-math",
            "∀ ∃ ∈ ∉ ∋  ∑ ∏ ∫ √ ∞  ≈ ≠ ≡ ≤ ≥  ⊕ ⊗ ⊂ ⊃", mathFont);

        BmpSection(doc, "Currency Symbols", "s-currency",
            "€  £  ¥  ¢  ₹  ₩  ₿  ₪  ₫  ₽", textFont);

        Section(doc, "Greek Alphabet", "s-greek");
        doc.Add(new TextBlock { Text = "α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ τ υ φ χ ψ ω", FontSize = 12, Margin = new Thickness(0, 0, 0, 2) });
        doc.Add(new TextBlock { Text = "Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω", FontSize = 12, Margin = new Thickness(0, 0, 0, 4) });

        BmpSection(doc, "Cyrillic", "s-cyrillic",
            "А Б В Г Д Е Ж З И К Л М Н О П Р С Т У Ф Х Ц Ч Ш Щ Ъ Ы Ь Э Ю Я", textFont);

        BmpSection(doc, "Miscellaneous Symbols (U+2600 – U+26FF)", "s-misc",
            "☀ ☁ ☂ ☃ ☄ ★ ☆ ♠ ♣ ♥ ♦ ☺ ☻ ♻ ⚡ ⚽ ⚾ ⛄ ⛅ ⛪ ⛽", symbolsFont);

        BmpSection(doc, "Dingbats (U+2700 – U+27BF)", "s-dingbats",
            "✀ ✁ ✂ ✃ ✄ ✆ ✇ ✈ ✉ ✊ ✋ ✌ ✍ ✎ ✏ ✐ ✑ ✒ ✓ ✗ ✘", symbolsFont);

        // ── Emoji sections ─────────────────────────────────────────────────────

        EmojiSection(doc, "Emoji Faces (U+1F600 – U+1F64F)", "s-faces",
            "😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇 🙂 😍 😎 🤩", emojiFont);

        EmojiSection(doc, "Hand Gestures", "s-hands",
            "👍 👎 👋 🤚 ✋ 🖐 👌 ✌ 🤞 👈 👉 👆 👇 👏", emojiFont);

        EmojiSection(doc, "Animals & Nature", "s-animals",
            "🐶 🐱 🐭 🐹 🐰 🦊 🐻 🐼 🐨 🐯 🦁 🐮 🐷 🐸", emojiFont);

        EmojiSection(doc, "Food & Drink", "s-food",
            "🍎 🍊 🍋 🍇 🍓 🍒 🍑 🥝 🍍 🥑 🌽 🥕 🍕 🍔", emojiFont);

        EmojiSection(doc, "Objects & Symbols", "s-objects",
            "🚀 🌈 🎉 🎵 💡 🔥 ⭐ 💯 🏆 🎯 📱 💻", emojiFont);

        // ── Mixed content ──────────────────────────────────────────────────────

        Section(doc, "Mixed Content", "s-mixed");

        doc.Add(new TextBlock
        {
            Text   = "Emoji appear inline with regular text using RichText spans — Latin text keeps its " +
                     "text-font metrics while each emoji span switches to the emoji font:",
            Margin = new Thickness(0, 0, 0, 6),
        });

        // NotoEmoji contains basic Latin glyphs on wide emoji advances, so setting it
        // as the whole line's font stretches the text — spans keep each script in the
        // font designed for it.
        TextStyle emojiStyle = new TextStyle().WithFamily(emojiFont);
        foreach (var (label, emoji, rest) in new[]
        {
            ("Status: ", "✅", " All systems operational"),
            ("Build: ", "🚀", " Deployed to production"),
            ("Tests: ", "🎯", " 245/245 passed"),
            ("Coverage: ", "📊", " 94.2%"),
            ("Performance: ", "⚡", " Response time < 100 ms"),
        })
        {
            doc.Add(new RichText { Margin = new Thickness(12, 0, 0, 4) }
                .Span(label)
                .Span(emoji, emojiStyle)
                .Span(rest));
        }

        return doc;
    }

    private static void Section(Document doc, string title, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontSize      = 13,
            TextColor     = ColorPalette.Indigo[600],
            Margin        = new Thickness(0, 12, 0, 4),
        });
    }

    private static void BmpSection(Document doc, string title, string tocId, string content, string font)
    {
        Section(doc, title, tocId);
        doc.Add(new TextBlock { Text = content, FontName = font, FontSize = 12, TextColor = ColorPalette.Slate[600], Margin = new Thickness(0, 0, 0, 4) });
    }

    private static void EmojiSection(Document doc, string title, string tocId, string content, string font)
    {
        Section(doc, title, tocId);
        doc.Add(new TextBlock { Text = content, FontName = font, FontSize = 20, TextColor = ColorPalette.Slate[800], Margin = new Thickness(0, 0, 0, 4) });
    }
}
