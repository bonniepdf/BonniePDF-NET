using System;
using System.IO;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;

namespace BonniePDF.Examples.Examples;

/// <summary>
/// Demonstrates supplementary Unicode plane support — emoji and symbols beyond U+FFFF
/// </summary>
public class EmojiExample
{
    public static string Generate(string outputPath = "emoji_example.pdf")
    {
        // Segoe UI Emoji is the standard Windows emoji font
        string emojiFont = @"C:\Windows\Fonts\seguiemj.ttf";

        if (!File.Exists(emojiFont))
        {
            Console.WriteLine("Segoe UI Emoji font not found. Skipping emoji example.");
            return null;
        }

        // Also use Arial for regular text
        string textFont = @"C:\Windows\Fonts\arial.ttf";
        if (!File.Exists(textFont))
            textFont = @"C:\Windows\Fonts\segoeui.ttf";

        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Emoji & Supplementary Unicode Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates characters beyond the Basic Multilingual Plane (U+FFFF)";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Emoji & Supplementary Unicode Support",
            FontName = textFont,
            FontSize = 22,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 8)
        });

        doc.Add(new TextBlock
        {
            Text = "BonniePDF supports the full Unicode range including characters beyond U+FFFF.",
            FontName = textFont,
            FontSize = 11,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 0, 0, 24)
        });

        // Section: Emoji faces
        AddSection(doc, textFont, emojiFont, "Emoji Faces", "😀 😃 😄 😁 😆 😅 😂 😊 😇 🙂");

        // Section: Hand gestures
        AddSection(doc, textFont, emojiFont, "Hand Gestures", "👍 👎 👋 👏 🙌 🤝 💪 ✌️");

        // Section: Animals & Nature
        AddSection(doc, textFont, emojiFont, "Animals & Nature", "🐶 🐱 🐭 🐹 🐰 🦊 🐻 🐼 🐨 🐯");

        // Section: Food & Drink
        AddSection(doc, textFont, emojiFont, "Food & Drink", "🍎 🍊 🍋 🍍 🍓 🍒 🍑 🍆 🥕 🌽");

        // Section: Travel & Places
        AddSection(doc, textFont, emojiFont, "Travel & Places", "🌍 🌎 🌏 🏔️ 🏖️ 🏝️ 🏞️ 🏟️");

        // Section: Flags & Symbols
        AddSection(doc, textFont, emojiFont, "Symbols", "❤️ 💡 🔥 ⭐ 🌈 🎉 🎵 🚀 ✨ 💯");

        // Section: Mixed text and emoji
        doc.Add(new TextBlock
        {
            Text = "Mixed Content",
            FontName = textFont,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 4)
        });

        doc.Add(new TextBlock
        {
            Text = "Emoji can be used inline with regular text using an emoji-capable font:",
            FontName = textFont,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 8)
        });

        string[] mixedLines =
        {
            "Status: ✅ All systems operational",
            "Build: 🚀 Deployed to production",
            "Tests: 🎯 245/245 passed",
            "Coverage: 📊 94.2%",
            "Performance: ⚡ Response time < 100ms"
        };

        foreach (string line in mixedLines)
        {
            doc.Add(new TextBlock
            {
                Text = line,
                FontName = emojiFont,
                FontSize = 13,
                TextColor = Color.Slate700,
                Margin = new Thickness(12, 0, 0, 4)
            });
        }

        doc.Save(outputPath);
        return outputPath;
    }

    private static void AddSection(Document doc, string textFont, string emojiFont, string title, string emojis)
    {
        doc.Add(new TextBlock
        {
            Text = title,
            FontName = textFont,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 16, 0, 4)
        });

        doc.Add(new TextBlock
        {
            Text = emojis,
            FontName = emojiFont,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 4)
        });
    }
}
