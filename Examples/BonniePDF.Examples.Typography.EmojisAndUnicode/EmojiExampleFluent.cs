using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;

namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

/// <summary>
/// Demonstrates supplementary Unicode plane support - emoji and symbols beyond U+FFFF -
/// using the Fluent API. Section headers inherit the text-font default;
/// emoji rows override with the emoji font per-item.
/// </summary>
[System.ComponentModel.DisplayName("Emojis & Unicode (Fluent API)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Rendering international text and emojis using the Fluent API.")]
public static class EmojiExampleFluent
{

    private static readonly string[] MixedContentLines =
    {
        "Status: ✅ All systems operational",
        "Build: 🚀 Deployed to production",
        "Tests: 🎯 245/245 passed",
        "Coverage: 📊 94.2%",
        "Performance: ⚡ Response time < 100ms"
    };

    public static Document Generate()
    {
        string emojiFont = @"C:\Windows\Fonts\seguiemj.ttf";
        if (!File.Exists(emojiFont))
        {
            Console.WriteLine("Segoe UI Emoji font not found. Skipping emoji example.");
            return null;
        }

        string textFont = @"C:\Windows\Fonts\arial.ttf";
        if (!File.Exists(textFont))
            textFont = @"C:\Windows\Fonts\segoeui.ttf";

        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Emoji & Supplementary Unicode Demo (Fluent)";
                metadata.Author = "BonniePDF";
                metadata.Subject = "Demonstrates characters beyond the Basic Multilingual Plane (U+FFFF)";
            });

            doc.WithSettings(settings =>
            {
                settings.Version = PdfVersion.Pdf17;
                // PDF/A compliance is disabled - emoji fonts often contain COLR/CBDT tables
                // that require relaxed validation and are incompatible with PDF/A-1b.
                settings.PdfAEnabled = false;
            });

            // Regular text font as document default; emoji rows override per item
            doc.WithDefaultTextStyle(style => style
                .WithFamily(textFont)
                .WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Emoji & Supplementary Unicode Support (Fluent API)")
                        .FontSize(22)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 8);

                    column.Item()
                        .Text("BonniePDF supports the full Unicode range including characters beyond U+FFFF.")
                        .TextColor(Color.Slate500)
                        .Margin(0, 0, 0, 24);

                    // ── Emoji sections ───────────────────────────────────────────
                    EmojiSection(column, textFont, emojiFont, "Emoji Faces",       "😀 😃 😄 😁 😆 😅 😂 😊 😇 🙂");
                    EmojiSection(column, textFont, emojiFont, "Hand Gestures",     "👍 👎 👋 👏 🙌 🤝 💪 ✌️");
                    EmojiSection(column, textFont, emojiFont, "Animals & Nature",  "🐶 🐱 🐭 🐹 🐰 🦊 🐻 🐼 🐨 🐯");
                    EmojiSection(column, textFont, emojiFont, "Food & Drink",      "🍎 🍊 🍋 🍍 🍓 🍒 🍑 🍆 🥕 🌽");
                    EmojiSection(column, textFont, emojiFont, "Travel & Places",   "🌍 🌎 🌏 🏔️ 🏖️ 🏝️ 🏞️ 🏟️");
                    EmojiSection(column, textFont, emojiFont, "Symbols",           "❤️ 💡 🔥 ⭐ 🌈 🎉 🎵 🚀 ✨ 💯");

                    // ── Mixed content ────────────────────────────────────────────
                    column.Item()
                        .Text("Mixed Content")
                        .FontSize(14)
                        .TextColor(Color.Slate700)
                        .Margin(0, 16, 0, 4);

                    column.Item()
                        .Text("Emoji can be used inline with regular text using an emoji-capable font:")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 8);

                    foreach (string line in MixedContentLines)
                    {
                        column.Item()
                            .Text(line)
                            .Font(emojiFont)   // emoji font handles both text and glyphs in these lines
                            .FontSize(13)
                            .TextColor(Color.Slate700)
                            .Margin(12, 0, 0, 4);
                    }
                });
            });
        }).Build();

    }

    /// <summary>
    /// Adds a section header followed by an emoji row to the column.
    /// The header inherits the document default (text font); the emoji row
    /// overrides with the emoji font explicitly.
    /// </summary>
    private static void EmojiSection(
        ColumnDescriptor column,
        string textFont,
        string emojiFont,
        string title,
        string emojis)
    {
        column.Item()
            .Text(title)
            .FontSize(14)
            .TextColor(Color.Slate700)
            .Margin(0, 16, 0, 4);

        column.Item()
            .Text(emojis)
            .Font(emojiFont)
            .FontSize(24)
            .TextColor(Color.Slate800)
            .Margin(0, 0, 0, 4);
    }
}

