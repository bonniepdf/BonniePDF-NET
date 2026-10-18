using BonniePDF;

namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

/// <summary>
/// Full Unicode range — BMP symbols, Greek, Cyrillic, dingbats, and supplementary emoji.
/// Keeps the Noto Emoji outline sample monochrome. PDF/UA-1 accessibility is applied.
/// </summary>
[System.ComponentModel.DisplayName("Emojis & Unicode")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description(
    "Full Unicode range — BMP symbols, Greek, Cyrillic, dingbats, and supplementary emoji across multiple embedded fonts. PDF/UA-1.")]
public static class EmojiExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        string textFont = Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSans-Regular.ttf");
        string mathFont = Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSansMath-Regular.ttf");
        string symbolsFont = Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSansSymbols2-Regular.ttf");
        string emojiFont = Path.Combine(AppContext.BaseDirectory, "fonts", "NotoEmoji-Regular.ttf");

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Emojis & Unicode",
                Author = "BonniePDF",
                Subject = "Full Unicode range including supplementary planes. PDF/UA-1.",
                Keywords = "pdf, pdf/ua, accessibility, emoji, unicode, fonts",
            },
            Settings = new DocumentSettings
            {
                // This example keeps its original monochrome outline appearance.
                EmojiRendering = EmojiRendering.Outline,
                Conformance = PdfAConformance.None,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(textFont)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[600]),
            },
        };

        // Keep the fallback faces on this document; generating it cannot alter another example.
        byte[] ReadFont(string path) => assets is not null && assets.TryGetValue("fonts/" + Path.GetFileName(path), out byte[]? bytes)
            ? bytes : File.ReadAllBytes(path);
        textFont = doc.Fonts.Add(ReadFont(textFont)).FontName;
        mathFont = doc.Fonts.Add(ReadFont(mathFont)).FontName;
        symbolsFont = doc.Fonts.Add(ReadFont(symbolsFont)).FontName;
        emojiFont = doc.Fonts.Add(ReadFont(emojiFont)).FontName;
        doc.Settings.DefaultTextStyle.WithFamily(textFont);

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Emojis & Supplementary Unicode")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = textFont,
            FontSize = 20,
            Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "BonniePDF embeds TrueType subsets for the full Unicode range. " +
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

        VerticalStack greek = Section("Greek Alphabet", "s-greek");
        greek.Add(new TextBlock
        {
            Text = "α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ τ υ φ χ ψ ω", FontSize = 12, Margin = new Thickness(0, 0, 0, 2)
        });
        greek.Add(new TextBlock
        {
            Text = "Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω", FontSize = 12, Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(greek);

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

        VerticalStack mixed = Section("Mixed Content", "s-mixed");

        mixed.Add(new TextBlock
        {
            Text = "Emoji appear inline with regular text using RichText spans — Latin text keeps its " +
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
            mixed.Add(new RichText { Margin = new Thickness(12, 0, 0, 4) }
                .Span(label)
                .Span(emoji, emojiStyle)
                .Span(rest));
        }

        doc.Add(mixed);

        return doc;
    }

    /// <summary>
    /// A section heading inside a keep-together block. Each section is added as one piece, so a
    /// heading never sits on a page apart from its content.
    /// </summary>
    private static VerticalStack Section(string title, string tocId)
    {
        VerticalStack stack = new() { BreakAllowed = false };
        stack.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level = 2,
            FontSize = 13,
            Color = ColorPalette.Indigo[600],
            Margin = new Thickness(0, 12, 0, 4),
        });
        return stack;
    }

    private static void BmpSection(Document doc, string title, string tocId, string content, string font)
    {
        VerticalStack section = Section(title, tocId);
        section.Add(new TextBlock
        {
            Text = content, FontName = font, FontSize = 12, Color = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(section);
    }

    private static void EmojiSection(Document doc, string title, string tocId, string content, string font)
    {
        VerticalStack section = Section(title, tocId);
        section.Add(new TextBlock
        {
            Text = content, FontName = font, FontSize = 20, Color = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 4)
        });
        doc.Add(section);
    }
}
