using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

/// <summary>
/// PDF/UA-1 tagged emoji-and-unicode example.
///
/// Emoji require a dedicated TTF font; this example embeds the bundled NotoEmoji
/// (monochrome outlines). PDF/A-1b is disabled because colour emoji fonts typically
/// contain COLR/CBDT tables incompatible with the PDF/A sRGB output-intent requirement.
/// PDF/UA-1 accessibility is enabled separately.
///
/// Section headers are /H2 structure elements; emoji and description paragraphs are /P.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Emojis & Unicode (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged emoji and supplementary Unicode - section headings tagged as /H2, emoji paragraphs as /P.")]
public static class EmojiTaggedExample
{
    public static Document? Generate()
    {
        // Embed the bundled NotoEmoji (glyphs) and NotoSans (text) - see BundledFont.
        string emojiFont = BundledFont.EmojiPath;
        string textFont = BundledFont.SansPath;

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Emoji & Unicode - Tagged (PDF/UA-1)",
                Author   = "BonniePDF",
                Subject  = "Demonstrates supplementary Unicode and emoji rendering in a PDF/UA-1 accessible document.",
                Keywords = "pdf, pdf/ua, accessibility, emoji, unicode",
            },
            Settings = new DocumentSettings
            {
                Version       = PdfVersion.Pdf17,
                // PDF/A is intentionally disabled: emoji colour fonts are incompatible with PDF/A-1b.
                PdfAEnabled   = false,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(textFont)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),
            },
        };

        doc.Add(new Heading("Emoji & Supplementary Unicode Support")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "BonniePDF supports the full Unicode range including characters beyond U+FFFF (the Supplementary Multilingual Planes). Each section below uses a semantic heading so screen readers can navigate the page.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        AddSection(doc, textFont, emojiFont, "Emoji Faces",
            "😀 😃 😄 😁 😆 😅 😂 😊 😇 🙂",
            "s-faces");

        AddSection(doc, textFont, emojiFont, "Hand Gestures",
            "👍 👎 👋 👏 🙌 🤝 💪 ✌️",
            "s-hands");

        AddSection(doc, textFont, emojiFont, "Animals & Nature",
            "🐶 🐱 🐭 🐹 🐰 🦊 🐻 🐼 🐨 🐯",
            "s-animals");

        AddSection(doc, textFont, emojiFont, "Food & Drink",
            "🍎 🍊 🍋 🍍 🍓 🍒 🍑 🍆 🥕 🌽",
            "s-food");

        AddSection(doc, textFont, emojiFont, "Symbols",
            "❤️ 💡 🔥 ⭐ 🌈 🎉 🎵 🚀 ✨ 💯",
            "s-symbols");

        // Mixed text and emoji
        doc.Add(new Heading("Mixed Content")
        {
            TocIdentifier = "s-mixed",
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 16, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text   = "Emoji can appear inline with regular text when an emoji-capable font is active:",
            Margin = new Thickness(0, 0, 0, 6),
        });

        string[] mixedLines =
        {
            "Status: ✅ All systems operational",
            "Build: 🚀 Deployed to production",
            "Tests: 🎯 245/245 passed",
            "Coverage: 📊 94.2%",
            "Performance: ⚡ Response time < 100 ms",
        };

        foreach (string line in mixedLines)
        {
            doc.Add(new TextBlock
            {
                Text      = line,
                FontName  = emojiFont,
                FontSize  = 13,
                TextColor = ColorPalette.Slate[700],
                Margin    = new Thickness(12, 0, 0, 4),
            });
        }

        return doc;
    }

    private static void AddSection(Document doc, string textFont, string emojiFont,
        string title, string emojis, string tocId)
    {
        doc.Add(new Heading(title)
        {
            TocIdentifier = tocId,
            Level         = 2,
            FontSize      = 14,
            TextColor     = ColorPalette.Slate[700],
            Margin        = new Thickness(0, 16, 0, 4),
        });

        doc.Add(new TextBlock
        {
            Text      = emojis,
            FontName  = emojiFont,
            FontSize  = 24,
            TextColor = ColorPalette.Slate[800],
            Margin    = new Thickness(0, 0, 0, 4),
        });
    }
}
